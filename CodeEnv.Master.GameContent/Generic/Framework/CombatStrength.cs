// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CombatStrength.cs
// Immutable data container holding values representing the offensive or defensive CombatStrength of MortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Immutable data container holding values representing the offensive or defensive Combat Strength of MortalItems.
    /// These values include damage infliction/mitigation and delivery vehicle survivability/interceptability.
    /// IMPROVE Still need to factor in reload time and accuracy. 
    /// IMPROVE need hull contribution to DamageMitigation.
    /// </summary>
    public struct CombatStrength : IEquatable<CombatStrength>, IComparable<CombatStrength> {

        private static string _noneToStringFormat = _offensiveToStringFormat + " : {8}";
        private static string _offensiveToStringFormat = "{0}[{1}]: {2}{3}, {4}{5}, {6}{7}";
        private static string _defensiveToStringFormat = "{0}[{1}]: {2}, {3}, {4} : {5}";

        private static string _offensiveToTextHudFormat = "{0}{1}, {2}{3}, {4}{5}";
        private static string _defensiveToTextHudFormat = "{0}, {1}, {2} : {3}";

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

            switch (left.Mode) {
                case CombatMode.Defensive:
                    var totalDamageMitigation = left.TotalDamageMitigation + right.TotalDamageMitigation;
                    return new CombatStrength(beamDeliveryStrength, projDeliveryStrength, missileDeliveryStrength, totalDamageMitigation);
                case CombatMode.Offensive:
                    var beamDamagePotential = left.BeamDamagePotential + right.BeamDamagePotential;
                    var projDamagePotential = left.ProjectileDamagePotential + right.ProjectileDamagePotential;
                    var missileDamagePotential = left.MissileDamagePotential + right.MissileDamagePotential;
                    return new CombatStrength(beamDeliveryStrength, projDeliveryStrength, missileDeliveryStrength, beamDamagePotential, projDamagePotential, missileDamagePotential);
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

        public CombatMode Mode { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of all the attacker's delivery vehicle(s) to survive interception.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to intercept and destroy all the attacker's delivery vehicle(s). 
        /// </summary>
        public float TotalDeliveryStrength { get { return BeamDeliveryStrength.Value + ProjectileDeliveryStrength.Value + MissileDeliveryStrength.Value; } }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's beam delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's shield to interdict and destroy the attacker's beam delivery vehicle(s). 
        /// </summary>
        public WDVStrength BeamDeliveryStrength { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's projectile delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to interdict and destroy the attacker's projectile delivery vehicle(s). 
        /// </summary>
        public WDVStrength ProjectileDeliveryStrength { get; private set; }

        /// <summary>
        /// If Mode is Offensive, this value represents the ability of the attacker's missile delivery vehicle(s) to survive interdiction.
        /// If Defensive, this value represents the ability of the defender's active countermeasure(s) to interdict and destroy the attacker's missile delivery vehicle(s). 
        /// </summary>
        public WDVStrength MissileDeliveryStrength { get; private set; }

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
        /// Only valid if the Mode is Offensive, this value represents the potential damage delivered to the defender by all the attacker's delivery vehicles.
        /// </summary>
        public DamageStrength TotalDamagePotential { get { return BeamDamagePotential + ProjectileDamagePotential + MissileDamagePotential; } }

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

            var deliveryStrengths = undamagedWeapons.Select(w => w.DeliveryVehicleStrength);
            BeamDeliveryStrength = CalcDeliveryStrength(deliveryStrengths, WDVCategory.Beam);
            ProjectileDeliveryStrength = CalcDeliveryStrength(deliveryStrengths, WDVCategory.Projectile);
            MissileDeliveryStrength = CalcDeliveryStrength(deliveryStrengths, WDVCategory.Missile);

            BeamDamagePotential = CalcDamagePotential(undamagedWeapons, WDVCategory.Beam);
            ProjectileDamagePotential = CalcDamagePotential(undamagedWeapons, WDVCategory.Projectile);
            MissileDamagePotential = CalcDamagePotential(undamagedWeapons, WDVCategory.Missile);
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
            var deliveryInterceptStrengths = undamagedActiveCMs.SelectMany(cm => cm.InterceptStrengths);
            BeamDeliveryStrength = CalcDeliveryStrength(deliveryInterceptStrengths, WDVCategory.Beam);
            ProjectileDeliveryStrength = CalcDeliveryStrength(deliveryInterceptStrengths, WDVCategory.Projectile);
            MissileDeliveryStrength = CalcDeliveryStrength(deliveryInterceptStrengths, WDVCategory.Missile);

            TotalDamageMitigation = undamagedCMs.Select(cm => cm.DamageMitigation).Aggregate(hullDamageMitigation, (accum, strength) => accum + strength);
        }

        /// <summary>
        /// Initializes a new offensive instance of the <see cref="CombatStrength"/> struct.
        /// </summary>
        /// <param name="beamDeliveryStrength">The beam delivery strength.</param>
        /// <param name="projDeliveryStrength">The proj delivery strength.</param>
        /// <param name="missileDeliveryStrength">The missile delivery strength.</param>
        /// <param name="beamDamagePotential">The beam damage potential.</param>
        /// <param name="projDamagePotential">The proj damage potential.</param>
        /// <param name="missileDamagePotential">The missile damage potential.</param>
        private CombatStrength(WDVStrength beamDeliveryStrength, WDVStrength projDeliveryStrength, WDVStrength missileDeliveryStrength,
            DamageStrength beamDamagePotential, DamageStrength projDamagePotential, DamageStrength missileDamagePotential)
            : this() {
            D.Assert(beamDeliveryStrength.Category == WDVCategory.Beam);
            D.Assert(projDeliveryStrength.Category == WDVCategory.Projectile);
            D.Assert(missileDeliveryStrength.Category == WDVCategory.Missile);

            Mode = CombatMode.Offensive;
            BeamDeliveryStrength = beamDeliveryStrength;
            ProjectileDeliveryStrength = projDeliveryStrength;
            MissileDeliveryStrength = missileDeliveryStrength;
            BeamDamagePotential = beamDamagePotential;
            ProjectileDamagePotential = projDamagePotential;
            MissileDamagePotential = missileDamagePotential;
        }

        /// <summary>
        /// Initializes a new defensive instance of the <see cref="CombatStrength"/> struct.
        /// </summary>
        /// <param name="beamInterceptStrength">The beam intercept strength.</param>
        /// <param name="projInterceptStrength">The proj intercept strength.</param>
        /// <param name="missileInterceptStrength">The missile intercept strength.</param>
        /// <param name="totalDamageMitigation">The total damage mitigation.</param>
        private CombatStrength(WDVStrength beamInterceptStrength, WDVStrength projInterceptStrength, WDVStrength missileInterceptStrength,
            DamageStrength totalDamageMitigation)
            : this() {
            D.Assert(beamInterceptStrength.Category == WDVCategory.Beam);
            D.Assert(projInterceptStrength.Category == WDVCategory.Projectile);
            D.Assert(missileInterceptStrength.Category == WDVCategory.Missile);

            Mode = CombatMode.Defensive;
            BeamDeliveryStrength = beamInterceptStrength;
            ProjectileDeliveryStrength = projInterceptStrength;
            MissileDeliveryStrength = missileInterceptStrength;
            TotalDamageMitigation = totalDamageMitigation;
        }

        private WDVStrength CalcDeliveryStrength(IEnumerable<WDVStrength> allDeliveryVehicleStrengths, WDVCategory deliveryVehicleCategory) {
            var vehicleStrengths = allDeliveryVehicleStrengths.Where(ds => ds.Category == deliveryVehicleCategory);
            var defaultValueIfEmpty = new WDVStrength(deliveryVehicleCategory, Constants.ZeroF);
            return vehicleStrengths.Aggregate(defaultValueIfEmpty, (accum, strength) => accum + strength);
        }

        private DamageStrength CalcDamagePotential(IEnumerable<AWeapon> weapons, WDVCategory deliveryVehicleCategory) {
            weapons.ForAll(w => D.Assert(!w.IsDamaged));
            var deliveryVehicleWeapons = weapons.Where(w => w.DeliveryVehicleCategory == deliveryVehicleCategory);
            var weaponsDamagePotential = deliveryVehicleWeapons.Select(w => w.DamagePotential);
            var defaultValueIfEmpty = default(DamageStrength);
            return weaponsDamagePotential.Aggregate(defaultValueIfEmpty, (accum, damagePotential) => accum + damagePotential);
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
                hash = hash * 31 + BeamDamagePotential.GetHashCode();
                hash = hash * 31 + ProjectileDamagePotential.GetHashCode();
                hash = hash * 31 + MissileDamagePotential.GetHashCode();
                hash = hash * 31 + TotalDamageMitigation.GetHashCode();
                return hash;
            }
        }

        #endregion

        public string ToTextHud() {
            if (Mode == CombatMode.Defensive) {
                return _defensiveToTextHudFormat.Inject(BeamDeliveryStrength.ToTextHud(), ProjectileDeliveryStrength.ToTextHud(),
                    MissileDeliveryStrength.ToTextHud(), TotalDamageMitigation.ToTextHud());
            }
            if (Mode == CombatMode.Offensive) {
                return _offensiveToTextHudFormat.Inject(BeamDeliveryStrength.ToTextHud(), BeamDamagePotential.ToTextHud(),
                    ProjectileDeliveryStrength.ToTextHud(), ProjectileDamagePotential.ToTextHud(), MissileDeliveryStrength.ToTextHud(),
                    MissileDamagePotential.ToTextHud());
            }
            return Mode.GetValueName();
        }

        public override string ToString() {
            if (Mode == CombatMode.Defensive) {
                return _defensiveToStringFormat.Inject(GetType().Name, Mode.GetValueName(), BeamDeliveryStrength, ProjectileDeliveryStrength, MissileDeliveryStrength, TotalDamageMitigation);
            }
            if (Mode == CombatMode.Offensive) {
                return _offensiveToStringFormat.Inject(GetType().Name, Mode.GetValueName(), BeamDeliveryStrength, BeamDamagePotential, ProjectileDeliveryStrength,
                   ProjectileDamagePotential, MissileDeliveryStrength, MissileDamagePotential);
            }
            return _noneToStringFormat.Inject(GetType().Name, Mode.GetValueName(), BeamDeliveryStrength, BeamDamagePotential, ProjectileDeliveryStrength,
                ProjectileDamagePotential, MissileDeliveryStrength, MissileDamagePotential, TotalDamageMitigation);
        }

        #region IEquatable<CombatStrength> Members

        public bool Equals(CombatStrength other) {
            return Mode == other.Mode && BeamDeliveryStrength == other.BeamDeliveryStrength
                && ProjectileDeliveryStrength == other.ProjectileDeliveryStrength && MissileDeliveryStrength == other.MissileDeliveryStrength
                && BeamDamagePotential == other.BeamDamagePotential && ProjectileDamagePotential == other.ProjectileDamagePotential
                && MissileDamagePotential == other.MissileDamagePotential && TotalDamageMitigation == other.TotalDamageMitigation;
            ;
        }

        #endregion

        #region IComparable<CombatStrength> Members

        public int CompareTo(CombatStrength other) {
            D.Assert(Mode != default(CombatMode) && Mode == other.Mode);
            if (Mode == CombatMode.Offensive) {
                return (TotalDamagePotential.Total + TotalDeliveryStrength).CompareTo(other.TotalDamagePotential.Total + other.TotalDeliveryStrength);
            }
            return (TotalDamageMitigation.Total + TotalDeliveryStrength).CompareTo(other.TotalDamageMitigation.Total + other.TotalDeliveryStrength);
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

