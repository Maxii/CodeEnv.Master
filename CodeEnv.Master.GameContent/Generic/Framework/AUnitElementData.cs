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

#define DEBUG_LOG
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

        private const string FullNameFormat = "{0}_{1}";

        public event EventHandler topographyChanged;

        public Priority HQPriority { get; private set; }

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

        private string _fullName;
        public override string FullName {
            get {
                if (ParentName.IsNullOrEmpty()) {
                    return Name;
                }
                if (_fullName.IsNullOrEmpty()) {
                    _fullName = FullNameFormat.Inject(ParentName, Name);
                }
                return _fullName;
            }
        }

        private AlertStatus _alertStatus;
        public AlertStatus AlertStatus {
            get { return _alertStatus; }
            set { SetProperty<AlertStatus>(ref _alertStatus, value, "AlertStatus", AlertStatusPropChangedHandler); }
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

        #region Initialization 

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
        /// <param name="hqPriority">The HQ priority.</param>
        public AUnitElementData(IUnitElement element, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, AHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators, Priority hqPriority)
            : base(element, owner, hullEquipment.MaxHitPoints, passiveCMs) {
            HullEquipment = hullEquipment;
            Mass = hullEquipment.Mass + hullEquipment.Weapons.Sum(w => w.Mass) + activeCMs.Sum(cm => cm.Mass) + sensors.Sum(s => s.Mass) + passiveCMs.Sum(cm => cm.Mass) + shieldGenerators.Sum(gen => gen.Mass);
            Expense = hullEquipment.Expense + hullEquipment.Weapons.Sum(w => w.Expense) + activeCMs.Sum(cm => cm.Expense) + sensors.Sum(s => s.Expense) + passiveCMs.Sum(cm => cm.Expense) + shieldGenerators.Sum(gen => gen.Expense);
            InitializeWeapons();
            Initialize(sensors);
            Initialize(activeCMs);
            Initialize(shieldGenerators);
            HQPriority = hqPriority;
        }

        private void InitializeWeapons() {
            // weapons are already present in hullEquipment
            //D.Log(ShowDebugLog, "{0} is about to initialize {1} Weapons.", FullName, Weapons.Count);
            Weapons.ForAll(weap => {
                D.AssertNotNull(weap.RangeMonitor);
                D.AssertNotNull(weap.WeaponMount);
                D.Assert(!weap.IsActivated);    // items control weapon activation during operations
                weap.isDamagedChanged += WeaponIsDamagedChangedEventHandler;
            });
        }

        private void Initialize(IEnumerable<ActiveCountermeasure> activeCMs) {
            ActiveCountermeasures = activeCMs.ToList();
            ActiveCountermeasures.ForAll(cm => {
                D.AssertNotNull(cm.RangeMonitor);
                D.Assert(!cm.IsActivated);    // items control countermeasures activation during operations
                cm.isDamagedChanged += CountermeasureIsDamagedChangedEventHandler;
            });
        }

        private void Initialize(IEnumerable<Sensor> sensors) {
            Sensors = sensors.ToList();
            Sensors.ForAll(s => {
                D.AssertNull(s.RangeMonitor);  // Note: Unlike Weapons and ActiveCountermeasures, Sensors are added to elements 
                // without a RangeMonitor attached. This is because the element adding the sensor does not yet have a Command attached 
                // and SensorRangeMonitors get attached to Cmds, not elements.
                D.Assert(!s.IsActivated);    // items control sensor activation when commencing operations
                s.isDamagedChanged += SensorIsDamagedChangedEventHandler;
            });
        }

        private void Initialize(IEnumerable<ShieldGenerator> generators) {
            ShieldGenerators = generators.ToList();
            ShieldGenerators.ForAll(gen => {
                D.AssertNotNull(gen.Shield);
                D.Assert(!gen.IsActivated);    // items control shield generator activation during operations
                gen.isDamagedChanged += ShieldGeneratorIsDamagedChangedEventHandler;
            });
        }

        #endregion

        public override void CommenceOperations() {
            base.CommenceOperations();
            Sensors.OrderBy(s => s.RangeCategory).ForAll(s => s.IsActivated = true);    // short, medium, long order
            // 11.3.16 Activation of ActiveCMs, ShieldGens and Weapons handled by HandleAlertStatusChanged

            RecalcSensorRange();
            RecalcDefensiveValues();
            RecalcOffensiveStrength();
            RecalcShieldRange();
            RecalcWeaponsRange();
        }

        #region Event and Property Change Handlers

        private void AlertStatusPropChangedHandler() {
            HandleAlertStatusChanged();
        }

        private void HandleAlertStatusChanged() {
            switch (AlertStatus) {
                case AlertStatus.Normal:
                    Weapons.ForAll(w => w.IsActivated = false);
                    ActiveCountermeasures.ForAll(acm => acm.IsActivated = false);
                    ShieldGenerators.ForAll(gen => gen.IsActivated = false);
                    break;
                case AlertStatus.Yellow:
                    Weapons.ForAll(w => w.IsActivated = false);
                    ActiveCountermeasures.ForAll(acm => acm.IsActivated = true);
                    ShieldGenerators.ForAll(gen => gen.IsActivated = true);
                    break;
                case AlertStatus.Red:
                    Weapons.ForAll(w => w.IsActivated = true);
                    ActiveCountermeasures.ForAll(acm => acm.IsActivated = true);
                    ShieldGenerators.ForAll(gen => gen.IsActivated = true);
                    break;
                case AlertStatus.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(AlertStatus));
            }
            // sensors stay activated to allow detection of enemy proximity by Cmd which determines AlertStatus
        }

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

        protected override void HandleTopographyChanged() {
            base.HandleTopographyChanged();
            OnTopographyChanged();
        }

        private void OnTopographyChanged() {
            if (topographyChanged != null) {
                topographyChanged(this, EventArgs.Empty);
            }
        }

        #endregion

        private void RecalcSensorRange() {
            var undamagedSensors = Sensors.Where(s => s.IsOperational);
            var shortRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Short);
            var mediumRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Medium);
            var longRangeSensors = undamagedSensors.Where(s => s.RangeCategory == RangeCategory.Long);
            float shortRangeDistance = shortRangeSensors.Any() ? shortRangeSensors.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeSensors.Any() ? mediumRangeSensors.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeSensors.Any() ? longRangeSensors.First().RangeDistance : Constants.ZeroF;
            SensorRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
            //D.Log(ShowDebugLog, "{0} recalculated SensorRange: {1}.", FullName, SensorRange);
        }

        private void RecalcWeaponsRange() {
            var undamagedWeapons = Weapons.Where(w => !w.IsDamaged);
            var shortRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Short);
            var mediumRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Medium);
            var longRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Long);
            //D.Log(ShowDebugLog, "{0} found {1} short, {2} medium and {3} long range undamaged weapons when recalculating WeaponsRange.", 
            //FullName, shortRangeWeapons.Count(), mediumRangeWeapons.Count(), longRangeWeapons.Count());
            float shortRangeDistance = shortRangeWeapons.Any() ? shortRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeWeapons.Any() ? mediumRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeWeapons.Any() ? longRangeWeapons.First().RangeDistance : Constants.ZeroF;
            WeaponsRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
            //D.Log(ShowDebugLog, "{0} recalculated WeaponsRange: {1}.", FullName, WeaponsRange);
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
            //D.Log(ShowDebugLog, "{0} recalculated ShieldRange: {1}.", FullName, ShieldRange);
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

