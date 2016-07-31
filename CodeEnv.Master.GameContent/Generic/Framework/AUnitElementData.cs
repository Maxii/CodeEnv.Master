// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitElementData.cs
// Abstract class for Data associated with an AUnitElementItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an AUnitElementItem.
    /// </summary>
    public abstract class AUnitElementData : AMortalItemData {

        public IList<AWeapon> Weapons { get { return HullEquipment.Weapons; } }
        public IList<Sensor> Sensors { get; private set; }
        public IList<ActiveCountermeasure> ActiveCountermeasures { get; private set; }
        public IList<ShieldGenerator> ShieldGenerators { get; private set; }

        public Vector3 HullDimensions { get { return HullEquipment.HullDimensions; } }

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
            private set { SetProperty<RangeDistance>(ref _weaponsRange, value, "WeaponsRange"); }
        }

        private RangeDistance _sensorRange;
        /// <summary>
        /// The RangeDistance profile of the sensors of this element.
        /// </summary>
        public RangeDistance SensorRange {
            get { return _sensorRange; }
            private set { SetProperty<RangeDistance>(ref _sensorRange, value, "SensorRange"); }
        }

        private RangeDistance _shieldRange;
        /// <summary>
        /// The RangeDistance profile of the shields of this element.
        /// </summary>
        public RangeDistance ShieldRange {
            get { return _shieldRange; }
            private set { SetProperty<RangeDistance>(ref _shieldRange, value, "ShieldRange"); }
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

        /// <summary>
        /// The mass of this Element.
        /// <remarks>7.26.16 Primarily here for user HUDs as an element's interaction with the physics
        /// engine uses the mass from the Rigidbody which is set one time in the UnitFactory.</remarks>
        /// </summary>
        public float Mass { get; private set; }

        protected AHullEquipment HullEquipment { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitElementItemData" /> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        public AUnitElementData(IUnitElement element, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, AHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators)
            : base(element, owner, hullEquipment.MaxHitPoints, passiveCMs) {
            HullEquipment = hullEquipment;
            Mass = hullEquipment.Mass + hullEquipment.Weapons.Sum(w => w.Mass) + activeCMs.Sum(cm => cm.Mass) + sensors.Sum(s => s.Mass) + passiveCMs.Sum(cm => cm.Mass) + shieldGenerators.Sum(gen => gen.Mass);
            Expense = hullEquipment.Expense + hullEquipment.Weapons.Sum(w => w.Expense) + activeCMs.Sum(cm => cm.Expense) + sensors.Sum(s => s.Expense) + passiveCMs.Sum(cm => cm.Expense) + shieldGenerators.Sum(gen => gen.Expense);
            InitializeWeapons();
            Initialize(sensors);
            Initialize(activeCMs);
            Initialize(shieldGenerators);
        }
        private void InitializeWeapons() {
            // weapons are already present in hullEquipment
            Weapons.ForAll(weap => {
                D.Assert(weap.RangeMonitor != null);
                D.Assert(weap.WeaponMount != null);
                D.Assert(!weap.IsActivated); // items activate equipment when the item commences operation
                weap.isDamagedChanged += WeaponIsDamagedChangedEventHandler;
            });
        }

        private void Initialize(IEnumerable<ActiveCountermeasure> activeCMs) {
            ActiveCountermeasures = activeCMs.ToList();
            ActiveCountermeasures.ForAll(cm => {
                D.Assert(cm.RangeMonitor != null);
                D.Assert(!cm.IsActivated);    // items activate equipment when the item commences operation
                cm.isDamagedChanged += CountermeasureIsDamagedChangedEventHandler;
            });
        }

        private void Initialize(IEnumerable<Sensor> sensors) {
            Sensors = sensors.ToList();
            Sensors.ForAll(s => {
                D.Assert(s.RangeMonitor == null);  // Note: Unlike Weapons and ActiveCountermeasures, Sensors are added to elements 
                // without a RangeMonitor attached. This is because the element adding the sensor does not yet have a Command attached 
                // and SensorRangeMonitors get attached to Cmds, not elements.
                D.Assert(!s.IsActivated);    // items activate equipment when the item commences operation
                s.isDamagedChanged += SensorIsDamagedChangedEventHandler;
            });
        }

        private void Initialize(IEnumerable<ShieldGenerator> generators) {
            ShieldGenerators = generators.ToList();
            ShieldGenerators.ForAll(gen => {
                D.Assert(gen.Shield != null);
                D.Assert(!gen.IsActivated);    // items activate equipment when the item commences operation
                gen.isDamagedChanged += ShieldGeneratorIsDamagedChangedEventHandler;
            });
        }

        public override void CommenceOperations() {
            base.CommenceOperations();
            Weapons.ForAll(w => w.IsActivated = true);
            ActiveCountermeasures.ForAll(cm => cm.IsActivated = true);
            Sensors.ForAll(s => s.IsActivated = true);
            ShieldGenerators.ForAll(gen => gen.IsActivated = true);
            RecalcDefensiveValues();
            RecalcOffensiveStrength();
            RecalcSensorRange();
            RecalcShieldRange();
            RecalcWeaponsRange();
        }

        #region Event and Property Change Handlers

        private void SensorIsDamagedChangedEventHandler(object sender, EventArgs e) {
            RecalcSensorRange();
        }

        private void WeaponIsDamagedChangedEventHandler(object sender, EventArgs e) {
            RecalcWeaponsRange();
            RecalcOffensiveStrength();
        }

        private void ShieldGeneratorIsDamagedChangedEventHandler(object sender, EventArgs e) {
            RecalcShieldRange();
            RecalcDefensiveValues();
        }

        #endregion

        private void RecalcSensorRange() {
            var undamagedSensors = Sensors.Where(s => s.IsOperational);
            var shortRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Short);
            var mediumRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Medium);
            var longRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeSensors.Any() ? shortRangeSensors.First().RangeDistance : Constants.ZeroF; //shortRangeSensors.CalcSensorRangeDistance();
            float mediumRangeDistance = mediumRangeSensors.Any() ? mediumRangeSensors.First().RangeDistance : Constants.ZeroF;  //mediumRangeSensors.CalcSensorRangeDistance();
            float longRangeDistance = longRangeSensors.Any() ? longRangeSensors.First().RangeDistance : Constants.ZeroF;    //longRangeSensors.CalcSensorRangeDistance();
            SensorRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        private void RecalcWeaponsRange() {
            var undamagedWeapons = Weapons.Where(w => !w.IsDamaged);
            var shortRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Short);
            var mediumRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Medium);
            var longRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeWeapons.Any() ? shortRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeWeapons.Any() ? mediumRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeWeapons.Any() ? longRangeWeapons.First().RangeDistance : Constants.ZeroF;
            WeaponsRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        private void RecalcShieldRange() {
            var undamagedGenerators = ShieldGenerators.Where(gen => !gen.IsDamaged);
            var shortRangeGenerators = undamagedGenerators.Where(gen => gen.RangeCategory == RangeCategory.Short);
            var mediumRangeGenerators = undamagedGenerators.Where(gen => gen.RangeCategory == RangeCategory.Medium);
            var longRangeGenerators = undamagedGenerators.Where(gen => gen.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeGenerators.Any() ? shortRangeGenerators.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeGenerators.Any() ? mediumRangeGenerators.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeGenerators.Any() ? longRangeGenerators.First().RangeDistance : Constants.ZeroF;
            ShieldRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
        }

        protected override void RecalcDefensiveValues() {
            List<ICountermeasure> allCountermeasures = new List<ICountermeasure>(PassiveCountermeasures.Cast<ICountermeasure>());
            allCountermeasures.AddRange(ActiveCountermeasures.Cast<ICountermeasure>());
            allCountermeasures.AddRange(ShieldGenerators.Cast<ICountermeasure>());
            var cmDamageMitigation = allCountermeasures.Where(cm => !cm.IsDamaged).Select(cm => cm.DamageMitigation).Aggregate(default(DamageStrength), (accum, cmDmgMit) => accum + cmDmgMit);
            DamageMitigation = HullEquipment.DamageMitigation + cmDamageMitigation;
            DefensiveStrength = new CombatStrength(allCountermeasures, HullEquipment.DamageMitigation);
        }

        private void RecalcOffensiveStrength() {
            OffensiveStrength = new CombatStrength(Weapons);
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            Weapons.ForAll(w => w.isDamagedChanged -= WeaponIsDamagedChangedEventHandler);
            Sensors.ForAll(s => s.isDamagedChanged -= SensorIsDamagedChangedEventHandler);
            ActiveCountermeasures.ForAll(cm => cm.isDamagedChanged -= CountermeasureIsDamagedChangedEventHandler);
            ShieldGenerators.ForAll(gen => gen.isDamagedChanged -= ShieldGeneratorIsDamagedChangedEventHandler);
        }

    }
}

