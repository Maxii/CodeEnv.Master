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

        public IList<AWeapon> Weapons { get { return HullEquipment.Weapons; } }
        public IList<Sensor> Sensors { get; private set; }
        public IList<ActiveCountermeasure> ActiveCountermeasures { get; private set; }
        public IList<ShieldGenerator> ShieldGenerators { get; private set; }

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

        private RangeDistance _shieldRange;
        /// <summary>
        /// The RangeDistance profile of the shields of this element.
        /// </summary>
        public RangeDistance ShieldRange {
            get { return _shieldRange; }
            set { SetProperty<RangeDistance>(ref _shieldRange, value, "ShieldRange"); }
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

        protected AHullEquipment HullEquipment { get; private set; }

        protected Rigidbody _rigidbody;

        /// <summary>
        /// Initializes a new instance of the <see cref="AUnitElementItemData" /> class.
        /// </summary>
        /// <param name="elementTransform">The element transform.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        public AUnitElementItemData(Transform elementTransform, AHullEquipment hullEquipment, Player owner, IEnumerable<ActiveCountermeasure> activeCMs,
            IEnumerable<Sensor> sensors, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<ShieldGenerator> shieldGenerators)
            : base(elementTransform, hullEquipment.Name, hullEquipment.MaxHitPoints, owner, passiveCMs) {
                _rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(elementTransform.gameObject);
            Initialize(hullEquipment, activeCMs, sensors, passiveCMs, shieldGenerators);
        }

        private void Initialize(AHullEquipment hullEquipment, IEnumerable<ActiveCountermeasure> activeCMs,
            IEnumerable<Sensor> sensors, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<ShieldGenerator> shieldGenerators) {
            HullEquipment = hullEquipment;
            float mass = hullEquipment.Mass + hullEquipment.Weapons.Sum(w => w.Mass) + activeCMs.Sum(cm => cm.Mass) + sensors.Sum(s => s.Mass) + passiveCMs.Sum(cm => cm.Mass) + shieldGenerators.Sum(gen => gen.Mass);
            Mass = mass;
            _rigidbody.mass = mass;

            Expense = hullEquipment.Expense + hullEquipment.Weapons.Sum(w => w.Expense) + activeCMs.Sum(cm => cm.Expense) + sensors.Sum(s => s.Expense) + passiveCMs.Sum(cm => cm.Expense) + shieldGenerators.Sum(gen => gen.Expense);

            InitializeWeapons();
            Initialize(sensors);
            Initialize(activeCMs);
            Initialize(shieldGenerators);
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

        private void InitializeWeapons() {
            // weapons are already present in hullEquipment
            Weapons.ForAll(weap => {
                D.Assert(weap.RangeMonitor != null);
                D.Assert(weap.WeaponMount != null);
                D.Assert(!weap.IsActivated); // items activate equipment when the item commences operation
                weap.onIsDamagedChanged += OnWeaponIsDamagedChanged;
            });
        }

        private void Initialize(IEnumerable<ActiveCountermeasure> activeCMs) {
            ActiveCountermeasures = activeCMs.ToList();
            ActiveCountermeasures.ForAll(cm => {
                D.Assert(cm.RangeMonitor != null);
                D.Assert(!cm.IsActivated);    // items activate equipment when the item commences operation
                cm.onIsDamagedChanged += OnCountermeasureIsDamagedChanged;
            });
        }

        private void Initialize(IEnumerable<Sensor> sensors) {
            Sensors = sensors.ToList();
            Sensors.ForAll(s => {
                D.Assert(s.RangeMonitor == null);  // Note: Unlike Weapons and ActiveCountermeasures, Sensors are added to elements 
                // without a RangeMonitor attached. This is because the element adding the sensor does not yet have a Command attached 
                // and SensorRangeMonitors get attached to Cmds, not elements.
                D.Assert(!s.IsActivated);    // items activate equipment when the item commences operation
                s.onIsDamagedChanged += OnSensorIsDamagedChanged;
            });
        }

        private void Initialize(IEnumerable<ShieldGenerator> generators) {
            ShieldGenerators = generators.ToList();
            ShieldGenerators.ForAll(gen => {
                D.Assert(gen.Shield != null);
                D.Assert(!gen.IsActivated);    // items activate equipment when the item commences operation
                gen.onIsDamagedChanged += OnShieldGeneratorIsDamagedChanged;
            });
        }

        private void OnIsHQChanged() {
            Name = IsHQ ? Name + _hqNameAddendum : Name.Remove(_hqNameAddendum);
        }

        private void OnSensorIsDamagedChanged(AEquipment sensor) {
            RecalcSensorRange();
        }

        private void OnWeaponIsDamagedChanged(AEquipment weapon) {
            RecalcWeaponsRange();
            RecalcOffensiveStrength();
        }

        private void OnShieldGeneratorIsDamagedChanged(AEquipment generator) {
            RecalcShieldRange();
            RecalcDefensiveValues();
        }

        private void RecalcSensorRange() {
            // CalcSensorRangeDistance filters out damage sensors
            var shortRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Short);
            var mediumRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Medium);
            var longRangeSensors = Sensors.Where(s => s.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeSensors.CalcSensorRangeDistance();
            float mediumRangeDistance = mediumRangeSensors.CalcSensorRangeDistance();
            float longRangeDistance = longRangeSensors.CalcSensorRangeDistance();
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
            Weapons.ForAll(w => w.onIsDamagedChanged -= OnWeaponIsDamagedChanged);
            Sensors.ForAll(s => s.onIsDamagedChanged -= OnSensorIsDamagedChanged);
            ActiveCountermeasures.ForAll(cm => cm.onIsDamagedChanged -= OnCountermeasureIsDamagedChanged);
            ShieldGenerators.ForAll(gen => gen.onIsDamagedChanged -= OnShieldGeneratorIsDamagedChanged);
        }

    }
}

