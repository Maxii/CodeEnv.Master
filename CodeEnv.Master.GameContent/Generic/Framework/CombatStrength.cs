﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStrength.cs
// Immutable data container holding values representing the offensive or defensive Combat Strength of MortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Immutable data container holding values representing the offensive or defensive Combat Strength of MortalItems.
    /// These values include damage infliction/mitigation and delivery vehicle survivability/interceptability.
    /// IMPROVE Still need to factor in reload time and accuracy. 
    /// </summary>
    public struct CombatStrength : IEquatable<CombatStrength>, IComparable<CombatStrength> {

        private const string NoneDebugNameFormat = OffensiveDebugNameFormat + " : {8}";
        private const string OffensiveDebugNameFormat = "{0}[{1}]: {2}{3}, {4}{5}, {6}{7}, {8}{9}";
        private const string DefensiveDebugNameFormat = "{0}[{1}]: {2}, {3}, {4}, {5} : {6}";

        private const string OffensiveToTextHudFormat = "{0}{1}, {2}{3}, {4}{5}, {6}{7}";
        private const string DefensiveToTextHudFormat = "{0}, {1}, {2}, {3} : {4}";

        #region Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(CombatStrength left, CombatStrength right) {
            return left.Equals(right);
        }

        public static bool operator !=(CombatStrength left, CombatStrength right) {
            return !left.Equals(right);
        }

        public static CombatStrength operator +(CombatStrength left, CombatStrength right) {
            if ((left.Mode == CombatMode.Defensive && right.Mode == CombatMode.Offensive) || (left.Mode == CombatMode.Offensive && right.Mode == CombatMode.Defensive)) {
                throw new System.ArithmeticException("LeftMode: {0}, RightMode: {1}".Inject(left.Mode.GetValueName(), right.Mode.GetValueName()));
            }

            var beamDeliveryStrength = left.BeamDeliveryStrength + right.BeamDeliveryStrength;
            var projDeliveryStrength = left.ProjectileDeliveryStrength + right.ProjectileDeliveryStrength;
            var missileDeliveryStrength = left.MissileDeliveryStrength + right.MissileDeliveryStrength;
            var assaultDeliveryStrength = left.AssaultDeliveryStrength + right.AssaultDeliveryStrength;

            switch (left.Mode) {
                case CombatMode.Defensive:
                    var totalDamageMitigation = left.TotalDamageMitigation + right.TotalDamageMitigation;
                    return new CombatStrength(beamDeliveryStrength, projDeliveryStrength, missileDeliveryStrength, assaultDeliveryStrength, totalDamageMitigation);
                case CombatMode.Offensive:
                    var beamDamagePotential = left.BeamDamagePotential + right.BeamDamagePotential;
                    var projDamagePotential = left.ProjectileDamagePotential + right.ProjectileDamagePotential;
                    var missileDamagePotential = left.MissileDamagePotential + right.MissileDamagePotential;
                    var assaultDamagePotential = left.AssaultDamagePotential + right.AssaultDamagePotential;
                    return new CombatStrength(beamDeliveryStrength, projDeliveryStrength, missileDeliveryStrength, assaultDeliveryStrength,
                        beamDamagePotential, projDamagePotential, missileDamagePotential, assaultDamagePotential);
                case CombatMode.None:
                    return right;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(left.Mode));
            }
        }

        /*************************************************************************************************************
                    * Operator Notes: 1. Addition operator only makes sense combining values of same Mode. Adding Offensive
                    * and Defensive to get a Total Strength value isn't very useful in the game.
                    * 2. Minus operator makes no sense as values that are valid differ between Offensive and Defensive
                    **************************************************************************************************************/
        #endregion

        public string DebugName {
            get {
                if (Mode == CombatMode.Defensive) {
                    return DefensiveDebugNameFormat.Inject(GetType().Name, Mode.GetValueName(), BeamDeliveryStrength, ProjectileDeliveryStrength, MissileDeliveryStrength, AssaultDeliveryStrength, TotalDamageMitigation);
                }
                if (Mode == CombatMode.Offensive) {
                    return OffensiveDebugNameFormat.Inject(GetType().Name, Mode.GetValueName(), BeamDeliveryStrength, BeamDamagePotential, ProjectileDeliveryStrength,
                       ProjectileDamagePotential, MissileDeliveryStrength, MissileDamagePotential, AssaultDeliveryStrength, AssaultDamagePotential);
                }
                return NoneDebugNameFormat.Inject(GetType().Name, Mode.GetValueName(), BeamDeliveryStrength, BeamDamagePotential, ProjectileDeliveryStrength,
                    ProjectileDamagePotential, MissileDeliveryStrength, MissileDamagePotential, AssaultDeliveryStrength, AssaultDamagePotential, TotalDamageMitigation);
            }
        }

        public CombatMode Mode { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of all the attacker's delivery vehicle(s) to survive interception.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to intercept and destroy all the attacker's delivery vehicle(s). 
        /// </summary>
        public DamageStrength TotalDeliveryStrength {
            get { return BeamDeliveryStrength + ProjectileDeliveryStrength + MissileDeliveryStrength + AssaultDeliveryStrength; }
        }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's beam delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's shield to interdict and destroy the attacker's beam delivery vehicle(s). 
        /// </summary>
        public DamageStrength BeamDeliveryStrength { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's projectile delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to interdict and destroy the attacker's projectile delivery vehicle(s). 
        /// </summary>
        public DamageStrength ProjectileDeliveryStrength { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's missile delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to interdict and destroy the attacker's missile delivery vehicle(s). 
        /// </summary>
        public DamageStrength MissileDeliveryStrength { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's assault delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to interdict and destroy the attacker's assault delivery vehicle(s). 
        /// </summary>
        public DamageStrength AssaultDeliveryStrength { get; private set; }


        /// <summary>
        /// Only valid if the Mode is Offensive, this value represents the potential damage delivered to the defender by the attacker's beam delivery vehicles.
        /// </summary>
        public DamageStrength BeamDamagePotential { get; private set; }

        /// <summary>
        /// Only valid if the Mode is Offensive, this value represents the potential damage delivered to the defender by the attacker's projectile delivery vehicles.
        /// </summary>
        public DamageStrength ProjectileDamagePotential { get; private set; }

        /// <summary>
        /// Only valid if the Mode is Offensive, this value represents the potential damage delivered to the defender by the attacker's missile delivery vehicles.
        /// </summary>
        public DamageStrength MissileDamagePotential { get; private set; }

        /// <summary>
        /// Only valid if the Mode is Offensive, this value represents the potential damage delivered to the defender by the attacker's assault delivery vehicles.
        /// </summary>
        public DamageStrength AssaultDamagePotential { get; private set; }


        /// <summary>
        /// Only valid if the Mode is Offensive, this value represents the potential damage delivered to the defender by all the attacker's delivery vehicles.
        /// </summary>
        public DamageStrength TotalDamagePotential {
            get { return BeamDamagePotential + ProjectileDamagePotential + MissileDamagePotential + AssaultDamagePotential; }
        }

        /// <summary>
        /// Only valid if the Mode is Defensive, this value represents the ability of the defender to mitigate the damage delivered by the attacker's delivery vehicles.
        /// </summary>
        public DamageStrength TotalDamageMitigation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatStrength" /> struct.
        /// Only undamaged weapons will be used from those provided.
        /// </summary>
        /// <param name="weapons">The weapons.</param>
        public CombatStrength(IEnumerable<AWeapon> weapons)
            : this() {
            Mode = CombatMode.Offensive;
            var undamagedWeapons = weapons.Where(w => !w.IsDamaged);

            BeamDeliveryStrength = CalcOrdnanceDeliveryStrength(undamagedWeapons, EquipmentCategory.BeamWeapon);
            ProjectileDeliveryStrength = CalcOrdnanceDeliveryStrength(undamagedWeapons, EquipmentCategory.ProjectileWeapon);
            MissileDeliveryStrength = CalcOrdnanceDeliveryStrength(undamagedWeapons, EquipmentCategory.MissileWeapon);
            AssaultDeliveryStrength = CalcOrdnanceDeliveryStrength(undamagedWeapons, EquipmentCategory.AssaultWeapon);

            BeamDamagePotential = CalcDamagePotential(undamagedWeapons, EquipmentCategory.BeamWeapon);
            ProjectileDamagePotential = CalcDamagePotential(undamagedWeapons, EquipmentCategory.ProjectileWeapon);
            MissileDamagePotential = CalcDamagePotential(undamagedWeapons, EquipmentCategory.MissileWeapon);
            AssaultDamagePotential = CalcDamagePotential(undamagedWeapons, EquipmentCategory.AssaultWeapon);
        }

        /// <summary>
        /// Initializes a new defensive instance of the <see cref="CombatStrength" /> struct.
        /// Only undamaged countermeasures will be used from those provided.
        /// </summary>
        /// <param name="countermeasures">The countermeasures.</param>
        /// <param name="hullDamageMitigation">The hull damage mitigation.</param>
        public CombatStrength(IEnumerable<ICountermeasure> countermeasures, DamageStrength hullDamageMitigation = default(DamageStrength))
            : this() {
            Mode = CombatMode.Defensive;
            var undamagedCMs = countermeasures.Where(cm => !cm.IsDamaged);

            var undamagedActiveCMs = undamagedCMs.Where(cm => cm is ActiveCountermeasure).Cast<ActiveCountermeasure>();
            BeamDeliveryStrength = CalcInterceptStrength(undamagedActiveCMs, EquipmentCategory.BeamWeapon);
            ProjectileDeliveryStrength = CalcInterceptStrength(undamagedActiveCMs, EquipmentCategory.ProjectileWeapon);
            MissileDeliveryStrength = CalcInterceptStrength(undamagedActiveCMs, EquipmentCategory.MissileWeapon);
            AssaultDeliveryStrength = CalcInterceptStrength(undamagedActiveCMs, EquipmentCategory.AssaultWeapon);

            TotalDamageMitigation = undamagedCMs.Select(cm => cm.DmgMitigation).Aggregate(hullDamageMitigation, (accum, strength) => accum + strength);
        }

        /// <summary>
        /// Initializes a new offensive instance of the <see cref="CombatStrength" /> struct.
        /// </summary>
        /// <param name="beamDeliveryStrength">The beam delivery strength.</param>
        /// <param name="projDeliveryStrength">The projectile delivery strength.</param>
        /// <param name="missileDeliveryStrength">The missile delivery strength.</param>
        /// <param name="assaultDeliveryStrength">The assault delivery strength.</param>
        /// <param name="beamDamagePotential">The beam damage potential.</param>
        /// <param name="projDamagePotential">The projectile damage potential.</param>
        /// <param name="missileDamagePotential">The missile damage potential.</param>
        /// <param name="assaultDamagePotential">The assault damage potential.</param>
        private CombatStrength(DamageStrength beamDeliveryStrength, DamageStrength projDeliveryStrength, DamageStrength missileDeliveryStrength,
            DamageStrength assaultDeliveryStrength, DamageStrength beamDamagePotential, DamageStrength projDamagePotential,
            DamageStrength missileDamagePotential, DamageStrength assaultDamagePotential) : this() {

            Mode = CombatMode.Offensive;
            BeamDeliveryStrength = beamDeliveryStrength;
            ProjectileDeliveryStrength = projDeliveryStrength;
            MissileDeliveryStrength = missileDeliveryStrength;
            AssaultDeliveryStrength = assaultDeliveryStrength;

            BeamDamagePotential = beamDamagePotential;
            ProjectileDamagePotential = projDamagePotential;
            MissileDamagePotential = missileDamagePotential;
            AssaultDamagePotential = assaultDamagePotential;
        }

        /// <summary>
        /// Initializes a new defensive instance of the <see cref="CombatStrength" /> struct.
        /// </summary>
        /// <param name="beamInterceptStrength">The beam intercept strength.</param>
        /// <param name="projInterceptStrength">The projectile intercept strength.</param>
        /// <param name="missileInterceptStrength">The missile intercept strength.</param>
        /// <param name="assaultInterceptStrength">The assault intercept strength.</param>
        /// <param name="totalDamageMitigation">The total damage mitigation.</param>
        private CombatStrength(DamageStrength beamInterceptStrength, DamageStrength projInterceptStrength, DamageStrength missileInterceptStrength,
            DamageStrength assaultInterceptStrength, DamageStrength totalDamageMitigation) : this() {

            Mode = CombatMode.Defensive;
            BeamDeliveryStrength = beamInterceptStrength;
            ProjectileDeliveryStrength = projInterceptStrength;
            MissileDeliveryStrength = missileInterceptStrength;
            AssaultDeliveryStrength = assaultInterceptStrength;
            TotalDamageMitigation = totalDamageMitigation;
        }

        private DamageStrength CalcOrdnanceDeliveryStrength(IEnumerable<AWeapon> undamagedWeapons, EquipmentCategory weapCat) {
            D.Assert(undamagedWeapons.All(w => !w.IsDamaged));
            var undamagedCatWeapons = undamagedWeapons.Where(w => w.Category == weapCat);
            IEnumerable<DamageStrength> ordDeliveryStrengths;
            if (weapCat == EquipmentCategory.BeamWeapon) {
                var beamWeapons = undamagedCatWeapons.Cast<BeamProjector>();
                ordDeliveryStrengths = beamWeapons.Select(w => w.BeamIntegrity);
            }
            else if (weapCat == EquipmentCategory.AssaultWeapon) {
                var assaultWeapons = undamagedCatWeapons.Cast<AssaultLauncher>();
                ordDeliveryStrengths = assaultWeapons.Select(w => w.OrdnanceDmgMitigation);
            }
            else if (weapCat == EquipmentCategory.MissileWeapon) {
                var missileWeapons = undamagedCatWeapons.Cast<MissileLauncher>();
                ordDeliveryStrengths = missileWeapons.Select(w => w.OrdnanceDmgMitigation);
            }
            else {
                D.AssertEqual(EquipmentCategory.ProjectileWeapon, weapCat);
                var projectileWeapons = undamagedCatWeapons.Cast<ProjectileLauncher>();
                ordDeliveryStrengths = projectileWeapons.Select(w => w.OrdnanceDmgMitigation);
            }
            var defaultValueIfEmpty = new DamageStrength();
            return ordDeliveryStrengths.Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private DamageStrength CalcInterceptStrength(IEnumerable<ActiveCountermeasure> undamagedCMs, EquipmentCategory weapCat) {
            D.Assert(undamagedCMs.All(cm => !cm.IsDamaged));
            // IMPROVE weapCat included to allow incorporation of interceptAccuracy that varies by weapCat
            IEnumerable<DamageStrength> interceptStrengths = undamagedCMs.Select(cm => cm.InterceptStrength);
            var defaultValueIfEmpty = new DamageStrength();
            return interceptStrengths.Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private DamageStrength CalcDamagePotential(IEnumerable<AWeapon> undamagedWeapons, EquipmentCategory weapCat) {
            D.Assert(undamagedWeapons.All(w => !w.IsDamaged));
            var catWeapons = undamagedWeapons.Where(w => w.Category == weapCat);
            var weaponsDamagePotential = catWeapons.Select(w => w.OrdnanceDmgPotential);
            var defaultValueIfEmpty = default(DamageStrength);
            return weaponsDamagePotential.Aggregate(defaultValueIfEmpty, (accum, damagePotential) => accum + damagePotential);
        }

        public DamageStrength GetStrength(EquipmentCategory category) {
            switch (category) {
                case EquipmentCategory.BeamWeapon:
                    return BeamDeliveryStrength;
                case EquipmentCategory.ProjectileWeapon:
                    return ProjectileDeliveryStrength;
                case EquipmentCategory.MissileWeapon:
                    return MissileDeliveryStrength;
                case EquipmentCategory.AssaultWeapon:
                    return AssaultDeliveryStrength;
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
            }
        }

        public string ToTextHud() {
            if (Mode == CombatMode.Defensive) {
                return DefensiveToTextHudFormat.Inject(BeamDeliveryStrength.ToTextHud(), ProjectileDeliveryStrength.ToTextHud(),
                    MissileDeliveryStrength.ToTextHud(), AssaultDeliveryStrength.ToTextHud(), TotalDamageMitigation.ToTextHud());
            }
            if (Mode == CombatMode.Offensive) {
                return OffensiveToTextHudFormat.Inject(BeamDeliveryStrength.ToTextHud(), BeamDamagePotential.ToTextHud(),
                    ProjectileDeliveryStrength.ToTextHud(), ProjectileDamagePotential.ToTextHud(), MissileDeliveryStrength.ToTextHud(),
                    MissileDamagePotential.ToTextHud(), AssaultDeliveryStrength.ToTextHud(), AssaultDamagePotential.ToTextHud());
            }
            return Mode.GetValueName();
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is CombatStrength)) { return false; }
            return Equals((CombatStrength)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See Page 254, C# 4.0 in a Nutshell.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Mode.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + BeamDeliveryStrength.GetHashCode();
                hash = hash * 31 + ProjectileDeliveryStrength.GetHashCode();
                hash = hash * 31 + MissileDeliveryStrength.GetHashCode();
                hash = hash * 31 + AssaultDeliveryStrength.GetHashCode();
                hash = hash * 31 + BeamDamagePotential.GetHashCode();
                hash = hash * 31 + ProjectileDamagePotential.GetHashCode();
                hash = hash * 31 + MissileDamagePotential.GetHashCode();
                hash = hash * 31 + AssaultDamagePotential.GetHashCode();
                hash = hash * 31 + TotalDamageMitigation.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() {
            return DebugName;
        }

        #region IEquatable<CombatStrength> Members

        public bool Equals(CombatStrength other) {
            return Mode == other.Mode && BeamDeliveryStrength == other.BeamDeliveryStrength
                && ProjectileDeliveryStrength == other.ProjectileDeliveryStrength && MissileDeliveryStrength == other.MissileDeliveryStrength
                && AssaultDeliveryStrength == other.AssaultDeliveryStrength && BeamDamagePotential == other.BeamDamagePotential
                && ProjectileDamagePotential == other.ProjectileDamagePotential && MissileDamagePotential == other.MissileDamagePotential
                && AssaultDamagePotential == other.AssaultDamagePotential && TotalDamageMitigation == other.TotalDamageMitigation;
            ;
        }

        #endregion

        #region IComparable<CombatStrength> Members

        public int CompareTo(CombatStrength other) {
            D.AssertNotDefault((int)Mode);
            D.AssertEqual(Mode, other.Mode);
            if (Mode == CombatMode.Offensive) {
                return (TotalDamagePotential + TotalDeliveryStrength).CompareTo(other.TotalDamagePotential + other.TotalDeliveryStrength);
            }
            return (TotalDamageMitigation + TotalDeliveryStrength).CompareTo(other.TotalDamageMitigation + other.TotalDeliveryStrength);
        }

        #endregion

        #region Nested Classes

        public enum CombatMode {
            None,
            Offensive,
            Defensive
        }

        #endregion
    }
}

