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

        public IList<AWeapon> Weapons { get; private set; }
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

        private RangeDistance _weaponsRange;
        /// <summary>
        /// The RangeDistance profile of the weapons of this element.
        /// </summary>
        public RangeDistance WeaponsRange {
            get { return _weaponsRange; }
            set { SetProperty<RangeDistance>(ref _weaponsRange, value, "WeaponsRange"); }
        }

        private RangeDistance _sensorRange;
        /// <summary>
        /// The RangeDistance profile of the sensors of this element.
        /// </summary>
        public RangeDistance SensorRange {
            get { return _sensorRange; }
            set { SetProperty<RangeDistance>(ref _sensorRange, value, "SensorRange"); }
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
            Weapons = new List<AWeapon>();
            Sensors = new List<Sensor>();
        }

        public void AddWeapon(AWeapon weapon) {
            D.Assert(weapon.RangeMonitor != null);
            D.Assert(!weapon.IsOperational);    // Items make weapons operational after completing the adding process
            D.Assert(!Weapons.Contains(weapon));
            Weapons.Add(weapon);
            weapon.onIsOperationalChanged += OnWeaponIsOperationalChanged;
            // no need to recalc weapons values as this occurs when IsOperational changes
        }

        public void AddSensor(Sensor sensor) {
            D.Assert(sensor.RangeMonitor == null);
            D.Assert(!sensor.IsOperational);    // Items make sensors operational after completing the adding process
            D.Assert(!Sensors.Contains(sensor));
            Sensors.Add(sensor);
            sensor.onIsOperationalChanged += OnSensorIsOperationalChanged;
            // no need to recalc sensor values as this occurs when IsOperational changes
        }

        /// <summary>
        /// Removes the weapon from the Element's collection of weapons.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        public void RemoveWeapon(AWeapon weapon) {
            D.Assert(Weapons.Contains(weapon));
            D.Assert(!weapon.IsOperational);    // Items make weapons non-operational when beginning the removal process
            Weapons.Remove(weapon);
            weapon.onIsOperationalChanged -= OnWeaponIsOperationalChanged;
            // no need to recalc weapon values as this occurs when IsOperational changes
        }

        public void RemoveSensor(Sensor sensor) {
            D.Assert(Sensors.Contains(sensor));
            D.Assert(!sensor.IsOperational);    // Items make sensors non-operational when beginning the removal process
            Sensors.Remove(sensor);
            sensor.onIsOperationalChanged -= OnSensorIsOperationalChanged;
            // no need to recalc sensor values as this occurs when IsOperational changes
        }

        private void OnIsHQChanged() {
            Name = IsHQ ? Name + _hqNameAddendum : Name.Remove(_hqNameAddendum);
        }

        private void OnSensorIsOperationalChanged(AEquipment sensor) {
            RecalcSensorRange();
        }

        private void OnWeaponIsOperationalChanged(AEquipment weapon) {
            RecalcWeaponsRange();
            RecalcOffensiveStrength();
        }

        private void RecalcSensorRange() {
            var shortRangeSensors = Sensors.Where(s => s.RangeCategory == RangeDistanceCategory.Short);
            var mediumRangeSensors = Sensors.Where(s => s.RangeCategory == RangeDistanceCategory.Medium);
            var longRangeSensors = Sensors.Where(s => s.RangeCategory == RangeDistanceCategory.Long);
            float shortRangeDistance = shortRangeSensors.CalcSensorRangeDistance();
            float mediumRangeDistance = mediumRangeSensors.CalcSensorRangeDistance();
            float longRangeDistance = longRangeSensors.CalcSensorRangeDistance();
            SensorRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        private void RecalcWeaponsRange() {
            var operationalWeapons = Weapons.Where(w => w.IsOperational);
            var shortRangeOpWeapons = operationalWeapons.Where(w => w.RangeCategory == RangeDistanceCategory.Short);
            var mediumRangeOpWeapons = operationalWeapons.Where(w => w.RangeCategory == RangeDistanceCategory.Medium);
            var longRangeOpWeapons = operationalWeapons.Where(w => w.RangeCategory == RangeDistanceCategory.Long);
            float shortRangeDistance = shortRangeOpWeapons.Any() ? shortRangeOpWeapons.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeOpWeapons.Any() ? mediumRangeOpWeapons.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeOpWeapons.Any() ? longRangeOpWeapons.First().RangeDistance : Constants.ZeroF;
            WeaponsRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
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

