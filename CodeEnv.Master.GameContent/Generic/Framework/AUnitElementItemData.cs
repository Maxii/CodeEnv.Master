// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementItemData.cs
// Abstract class for Data associated with an AUnitElementItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AUnitElementItem.
    /// </summary>
    public abstract class AUnitElementItemData : AMortalItemData {

        private static string _hqNameAddendum = "[HQ]";

        public IList<Weapon> Weapons { get; private set; }
        public IList<Sensor> Sensors { get; private set; }

        private bool _isHQ;
        public virtual bool IsHQ {
            get { return _isHQ; }
            set { SetProperty<bool>(ref _isHQ, value, "IsHQ", OnIsHQChanged); }
        }

        private string _parentName;
        public string ParentName {
            get { return _parentName; }
            set { SetProperty<string>(ref _parentName, value, "ParentName"); }
        }

        public override string FullName {
            get { return ParentName.IsNullOrEmpty() ? Name : ParentName + Constants.Underscore + Name; }
        }

        private CombatStrength _offensiveStrength;
        public CombatStrength OffensiveStrength {
            get { return _offensiveStrength; }
            private set { SetProperty<CombatStrength>(ref _offensiveStrength, value, "OffensiveStrength"); }
        }

        private float _maxWeaponsRange;
        public float MaxWeaponsRange {
            get { return _maxWeaponsRange; }
            private set { SetProperty<float>(ref _maxWeaponsRange, value, "MaxWeaponsRange"); }
        }

        private float _maxSensorRange;
        public float MaxSensorRange {
            get { return _maxSensorRange; }
            set { SetProperty<float>(ref _maxSensorRange, value, "MaxSensorRange"); }
        }

        private float _science;
        public float Science {
            get { return _science; }
            set { SetProperty<float>(ref _science, value, "Science"); }
        }

        private float _culture;
        public float Culture {
            get { return _culture; }
            set { SetProperty<float>(ref _culture, value, "Culture"); }
        }

        private float _income;
        public float Income {
            get { return _income; }
            set { SetProperty<float>(ref _income, value, "Income"); }
        }

        private float _expense;
        public float Expense {
            get { return _expense; }
            set { SetProperty<float>(ref _expense, value, "Expense"); }
        }

        public float Mass { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitElementItemData" /> class.
        /// </summary>
        /// <param name="elementTransform">The element transform.</param>
        /// <param name="name">The name of the Element.</param>
        /// <param name="mass">The mass of the Element.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="owner">The owner.</param>
        public AUnitElementItemData(Transform elementTransform, string name, float mass, float maxHitPoints, Player owner)
            : base(elementTransform, name, maxHitPoints, owner) {
            Mass = mass;
            elementTransform.rigidbody.mass = mass;
            Weapons = new List<Weapon>();
            Sensors = new List<Sensor>();
        }

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

        private void OnIsHQChanged() {
            Name = IsHQ ? Name + _hqNameAddendum : Name.Remove(_hqNameAddendum);
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

