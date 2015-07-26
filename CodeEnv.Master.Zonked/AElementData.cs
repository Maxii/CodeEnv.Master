// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AElementData.cs
// Abstract base class that holds data for Items that are elements of a command.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class that holds data for Items that are elements of a command.
    /// </summary>
    public abstract class AElementData : AMortalItemData {

        public IList<AWeapon> Weapons { get; private set; }
        public IList<Sensor> Sensors { get; private set; }

        private CombatStrength _offensiveStrength;
        public CombatStrength OffensiveStrength {
            get { return _offensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _offensiveStrength, value, "OffensiveStrength"); }
        }

        private float _maxWeaponsRange;
        /// <summary>
        /// The maximum range of this item's weapons.
        /// </summary>
        public float MaxWeaponsRange {
            get { return _maxWeaponsRange; }
            private set { SetProperty<float>(ref _maxWeaponsRange, value, "MaxWeaponsRange"); }
        }

        private float _maxSensorRange;
        /// <summary>
        /// The maximum range of this item's sensors.
        /// </summary>
        /// <value>
        /// The maximum sensor range.
        /// </value>
        public float MaxSensorRange {
            get { return _maxSensorRange; }
            set { SetProperty<float>(ref _maxSensorRange, value, "MaxSensorRange"); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitElementData" /> class.
        /// </summary>
        /// <param name="name">The name of the Element.</param>
        /// <param name="mass">The mass of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        public AUnitElementData(string name, float mass, float maxHitPoints)
            : base(name, mass, maxHitPoints) {
            Weapons = new List<AWeapon>();
            Sensors = new List<Sensor>();
        }

        /// <summary>
        /// Adds the weapon to this element data.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        public void AddWeapon(Weapon weapon) {
            D.Assert(weapon.RangeMonitor != null);
            D.Assert(!weapon.IsOperational);
            D.Assert(!Weapons.Contains(weapon));
            Weapons.Add(weapon);
            weapon.onIsOperationalChanged += OnWeaponIsOperationalChanged;
            // no need to Recalc max weapon-related values as this occurs when IsOperational changes
        }

        public void AddSensor(Sensor sensor) {
            D.Assert(sensor.RangeMonitor == null);
            D.Assert(!sensor.IsOperational);
            D.Assert(!Sensors.Contains(sensor));
            Sensors.Add(sensor);
            sensor.onIsOperationalChanged += OnSensorIsOperationalChanged;
        }

        /// <summary>
        /// Removes the weapon from the Element's collection of weapons.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        public void RemoveWeapon(Weapon weapon) {
            D.Assert(Weapons.Contains(weapon));
            D.Assert(!weapon.IsOperational);
            Weapons.Remove(weapon);
            weapon.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
            // no need to Recalc max weapon-related values as this occurs when IsOperational changes
        }

        public void RemoveSensor(Sensor sensor) {
            D.Assert(Sensors.Contains(sensor));
            D.Assert(!sensor.IsOperational);
            Sensors.Remove(sensor);
            sensor.onIsOperationalChanged -= OnSensorIsOperationalChanged;
        }

        private void OnSensorIsOperationalChanged(Sensor sensor) {
            RecalcMaxSensorRange();
        }

        private void OnWeaponIsOperationalChanged(Weapon weapon) {
            RecalcMaxWeaponsRange();
            RecalcOffensiveStrength();
        }

        private void RecalcMaxSensorRange() {
            var operationalSensors = Sensors.Where(s => s.IsOperational);
            MaxSensorRange = operationalSensors.Any() ? operationalSensors.Max(s => s.Range.GetSensorRange(Owner)) : Constants.ZeroF;
        }

        private void RecalcMaxWeaponsRange() {
            var operationalWeapons = Weapons.Where(w => w.IsOperational);
            MaxWeaponsRange = operationalWeapons.Any() ? operationalWeapons.Max(w => w.Range.GetWeaponRange(Owner)) : Constants.ZeroF;
        }

        private void RecalcOffensiveStrength() {
            var defaultValueIfEmpty = default(CombatStrength);
            OffensiveStrength = Weapons.Where(weap => weap.IsOperational).Select(weap => weap.Strength).Aggregate(defaultValueIfEmpty, (accum, wStrength) => accum + wStrength);
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            Weapons.ForAll(w => w.onIsOperationalChanged -= OnWeaponIsOperationalChanged);
        }

    }
}

