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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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

        private const string DebugNameFormat = "{0}'s {1}.{2}";

        public event EventHandler topographyChanged;

        public event EventHandler<WeaponsRangeChangingEventArgs> weaponsRangeChanging;

        public Priority HQPriority { get { return Design.HQPriority; } }

        public IList<AWeapon> Weapons { get { return HullEquipment.Weapons; } }
        public IList<ElementSensor> Sensors { get; private set; }
        public IList<ActiveCountermeasure> ActiveCountermeasures { get; private set; }
        public IList<ShieldGenerator> ShieldGenerators { get; private set; }

        public Vector3 HullDimensions { get { return HullEquipment.HullDimensions; } }

        public string UnitName { get; set; }

        public override string DebugName {
            get {
                if (Owner != null) {
                    return DebugNameFormat.Inject(Owner.DebugName, UnitName, Name);
                }
                return DebugNameFormat.Inject(Constants.Empty, UnitName, Name);
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
            private set { SetProperty<RangeDistance>(ref _weaponsRange, value, "WeaponsRange", onChanging: WeaponsRangePropChangingHandler); }
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

        private OutputsYield _outputs;
        public OutputsYield Outputs {
            get { return _outputs; }
            set { SetProperty<OutputsYield>(ref _outputs, value, "Outputs"); }
        }

        /// <summary>
        /// The current design of this Element.
        /// <remarks>Public set as must be changeable to the intended refit design from ExecuteRefitOrder state.</remarks>
        /// </summary>
        public AUnitElementDesign Design { get; set; }

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
        /// <param name="design">The design.</param>
        public AUnitElementData(IUnitElement element, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, AHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<ElementSensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            AUnitElementDesign design)
            : base(element, owner, hullEquipment.MaxHitPoints, passiveCMs) {
            HullEquipment = hullEquipment;
            Mass = hullEquipment.Mass + hullEquipment.Weapons.Sum(w => w.Mass) + activeCMs.Sum(cm => cm.Mass) + sensors.Sum(s => s.Mass) + passiveCMs.Sum(cm => cm.Mass) + shieldGenerators.Sum(gen => gen.Mass);
            InitializeWeapons();
            Initialize(sensors);
            Initialize(activeCMs);
            Initialize(shieldGenerators);
            Design = design;
        }

        private void InitializeWeapons() {
            // weapons are already present in hullEquipment
            //D.Log(ShowDebugLog, "{0} is about to initialize {1} Weapons.", DebugName, Weapons.Count);
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

        private void Initialize(IEnumerable<ElementSensor> sensors) {
            Sensors = sensors.ToList();
            Sensors.ForAll(s => {
                D.AssertNotNull(s.RangeMonitor);  // 3.31.17 Sensors now have their monitor attached when built in UnitFactory
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
            // 11.3.16 Activation of ActiveCMs, ShieldGens and Weapons handled by HandleAlertStatusChanged
            RecalcDefensiveValues();
            RecalcOffensiveStrength();
            RecalcShieldRange();
            RecalcWeaponsRange();
        }

        /// <summary>
        /// Activates the Element's SRSensors.
        /// <remarks>5.13.17 Moved from Data.CommenceOperations to allow Element.CommenceOperations to call when
        /// it is prepared to detect and be detected - aka after it enters Idling state.</remarks>
        /// </summary>
        public void ActivateSRSensors() {
            Sensors.ForAll(s => s.IsActivated = true);
            RecalcSensorRange();
        }

        /// <summary>
        /// Deactivates the Element's SRSensors.
        /// <remarks>11.3.17 Used primarily to allow Ship to deactivate its SRSensors when the Ship's
        /// Command is nulled either temporarily (in transition from one command to another) or semi-permanently
        /// when it is initially constructed or refitted at a BaseHanger. Otherwise, active SRSensors will try to
        /// communicate with FleetCommand's UnifiedSRSensorMonitor which is not there.</remarks>
        /// </summary>
        public void DeactivateSRSensors() {
            Sensors.ForAll(s => s.IsActivated = false);
            RecalcSensorRange();
        }

        #region Initial Construction and Refitting

        public void PrepareForInitialConstruction() {
            // change the values to what they should be during construction
            DamageNonHullEquipment(Constants.OneHundredPercent);
            Outputs *= TempGameValues.UnderConstructionValuesScaler;
            // All other Element-specific Properties are changed as a result of DamageEquipment
            float maxAllowedCurrentHitPts = MaxHitPoints * TempGameValues.UnderConstructionValuesScaler;
            CurrentHitPoints = CurrentHitPoints < maxAllowedCurrentHitPts ? CurrentHitPoints : maxAllowedCurrentHitPts;
        }

        public virtual void RestoreInitialConstructionValues() {
            // Outputs regenerated from equipment in derived classes
            RemoveDamageFromAllEquipment();
            CurrentHitPoints = MaxHitPoints;
        }

        public PreReworkValuesStorage PrepareForRework() {
            // store the values that will need to be restored if Rework is canceled
            var reworkStorage = new PreReworkValuesStorage(Design, CurrentHitPoints);
            // damage the equipment and store what was damaged
            var damagedEquipment = DamageNonHullEquipment(Constants.OneHundredPercent);
            reworkStorage.EquipmentDamaged = damagedEquipment;
            // Outputs will be regenerated from equipment in derived classes if canceled

            // change the values to what they should be during Rework
            Outputs *= TempGameValues.UnderConstructionValuesScaler;
            // All other Element-specific Properties are changed as a result of DamageNonHullEquipment
            float maxAllowedCurrentHitPts = MaxHitPoints * TempGameValues.UnderConstructionValuesScaler;
            CurrentHitPoints = CurrentHitPoints < maxAllowedCurrentHitPts ? CurrentHitPoints : maxAllowedCurrentHitPts;
            return reworkStorage;
        }

        public virtual void RestorePreReworkValues(PreReworkValuesStorage preReworkValues) {
            // UNCLEAR currently restoring previous values, but consider leaving most as is, requiring repair
            Design = preReworkValues.Design;
            CurrentHitPoints = preReworkValues.CurrentHitPts;
            preReworkValues.EquipmentDamaged.RestoreUndamagedState();
            preReworkValues.WasUsedToRestorePreReworkValues = true;
            // Outputs regenerated from equipment in derived classes
        }

        // Refit completion is handled by creating an upgraded UnitElementItem

        /// <summary>
        /// Damages non-hull equipment including CMs, ShieldGenerators, Weapons and Sensors. 
        /// The equipment damaged is determined by 1) whether its damageable, 2) how many of each type are kept 
        /// undamaged to simulate a minimal defense while being reworked, and 3) the damagePercent.
        /// <remarks>This approach allows a degree of control on the availability of equipment
        /// as rework proceeds and doesn't interfere with use of operational equipment when AlertLevel changes.</remarks>
        /// </summary>
        /// <param name="damagePercent">The damage percent.</param>
        public virtual EquipmentDamagedFromRework DamageNonHullEquipment(float damagePercent) {
            Utility.ValidateForRange(damagePercent, Constants.ZeroPercent, Constants.OneHundredPercent);

            var equipDamagedFromRework = new EquipmentDamagedFromRework();
            var damageablePCMs = PassiveCountermeasures.Where(pcm => pcm.IsDamageable).Skip(1);
            var pCMsToBeDamaged = damageablePCMs.Where(pcm => RandomExtended.Chance(damagePercent));
            equipDamagedFromRework.PassiveCMs = pCMsToBeDamaged;

            var damageableACMs = ActiveCountermeasures.Where(acm => acm.IsDamageable).Skip(1);
            var aCMsToBeDamaged = damageableACMs.Where(acm => RandomExtended.Chance(damagePercent));
            equipDamagedFromRework.ActiveCMs = aCMsToBeDamaged;

            var damageableSGs = ShieldGenerators.Where(sg => sg.IsDamageable).Skip(1);
            var sGsToBeDamaged = damageableSGs.Where(sg => RandomExtended.Chance(damagePercent));
            equipDamagedFromRework.ShieldGenerators = sGsToBeDamaged;

            var damageableWeaps = Weapons.Where(w => w.IsDamageable).Skip(1);
            var weapsToBeDamaged = damageableWeaps.Where(weap => RandomExtended.Chance(damagePercent));
            equipDamagedFromRework.Weapons = weapsToBeDamaged;

            var damageableSensors = Sensors.Where(s => s.IsDamageable).Skip(1);
            var sensorsToBeDamaged = damageableSensors.Where(s => RandomExtended.Chance(damagePercent));
            equipDamagedFromRework.Sensors = sensorsToBeDamaged;

            RemoveDamageFromAllEquipment();
            pCMsToBeDamaged.ForAll(pcm => pcm.IsDamaged = true);
            aCMsToBeDamaged.ForAll(acm => acm.IsDamaged = true);
            sGsToBeDamaged.ForAll(sg => sg.IsDamaged = true);
            weapsToBeDamaged.ForAll(w => w.IsDamaged = true);
            sensorsToBeDamaged.ForAll(s => s.IsDamaged = true);

            return equipDamagedFromRework;
        }

        public virtual void RemoveDamageFromAllEquipment() {
            PassiveCountermeasures.Where(cm => cm.IsDamageable).ForAll(cm => cm.IsDamaged = false);
            ActiveCountermeasures.Where(cm => cm.IsDamageable).ForAll(cm => cm.IsDamaged = false);
            ShieldGenerators.Where(gen => gen.IsDamageable).ForAll(gen => gen.IsDamaged = false);
            Weapons.Where(w => w.IsDamageable).ForAll(w => w.IsDamaged = false);
            Sensors.Where(s => s.IsDamageable).ForAll(s => s.IsDamaged = false);
        }

        #endregion

        #region Event and Property Change Handlers

        private void AlertStatusPropChangedHandler() {
            HandleAlertStatusChanged();
        }

        private void SensorIsDamagedChangedEventHandler(object sender, EventArgs e) {
            HandleSensorIsDamagedChanged(sender as ElementSensor);
        }

        private void WeaponIsDamagedChangedEventHandler(object sender, EventArgs e) {
            HandleWeaponIsDamagedChanged(sender as AWeapon);
        }

        private void ShieldGeneratorIsDamagedChangedEventHandler(object sender, EventArgs e) {
            HandleShieldGeneratorIsDamagedChanged(sender as ShieldGenerator);
        }

        private void WeaponsRangePropChangingHandler(RangeDistance incomingWeaponsRange) {
            OnWeaponsRangeChanging(incomingWeaponsRange);
        }

        private void OnTopographyChanged() {
            if (topographyChanged != null) {
                topographyChanged(this, EventArgs.Empty);
            }
        }

        private void OnWeaponsRangeChanging(RangeDistance incomingWeaponsRange) {
            if (weaponsRangeChanging != null) {
                weaponsRangeChanging(this, new WeaponsRangeChangingEventArgs(incomingWeaponsRange));
            }
        }

        #endregion

        private void HandleSensorIsDamagedChanged(ElementSensor sensor) {
            D.Log(ShowDebugLog, "{0}'s {1} is {2}.", DebugName, sensor.Name, sensor.IsDamaged ? "damaged" : "repaired");
            RecalcSensorRange();
        }

        private void HandleWeaponIsDamagedChanged(AWeapon weapon) {
            D.Log(ShowDebugLog, "{0}'s {1} is {2}.", DebugName, weapon.Name, weapon.IsDamaged ? "damaged" : "repaired");
            RecalcWeaponsRange();
            RecalcOffensiveStrength();
        }

        private void HandleShieldGeneratorIsDamagedChanged(ShieldGenerator sGen) {
            D.Log(ShowDebugLog, "{0}'s {1} is {2}.", DebugName, sGen.Name, sGen.IsDamaged ? "damaged" : "repaired");
            RecalcShieldRange();
            RecalcDefensiveValues();
        }

        protected override void HandleTopographyChanged() {
            base.HandleTopographyChanged();
            OnTopographyChanged();
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

        private void RecalcSensorRange() {
            var undamagedSRSensors = Sensors.Where(s => !s.IsDamaged);
            float shortRangeDistance = undamagedSRSensors.Any() ? undamagedSRSensors.First().RangeDistance : Constants.ZeroF;
            SensorRange = new RangeDistance(shortRangeDistance, Constants.ZeroF, Constants.ZeroF);
            //D.Log(ShowDebugLog, "{0} recalculated SensorRange: {1}.", DebugName, SensorRange);
        }

        private void RecalcWeaponsRange() {
            var undamagedWeapons = Weapons.Where(w => !w.IsDamaged);
            var shortRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Short);
            var mediumRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Medium);
            var longRangeWeapons = undamagedWeapons.Where(w => w.RangeCategory == RangeCategory.Long);
            //D.Log(ShowDebugLog, "{0} found {1} short, {2} medium and {3} long range undamaged weapons when recalculating WeaponsRange.", 
            //DebugName, shortRangeWeapons.Count(), mediumRangeWeapons.Count(), longRangeWeapons.Count());
            float shortRangeDistance = shortRangeWeapons.Any() ? shortRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float mediumRangeDistance = mediumRangeWeapons.Any() ? mediumRangeWeapons.First().RangeDistance : Constants.ZeroF;
            float longRangeDistance = longRangeWeapons.Any() ? longRangeWeapons.First().RangeDistance : Constants.ZeroF;
            WeaponsRange = new RangeDistance(shortRangeDistance, mediumRangeDistance, longRangeDistance);
            //D.Log(ShowDebugLog, "{0} recalculated WeaponsRange: {1}.", DebugName, WeaponsRange);
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
            //D.Log(ShowDebugLog, "{0} recalculated ShieldRange: {1}.", DebugName, ShieldRange);
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

        /// <summary>
        /// Called on the element that was refitted and therefore replaced.
        /// <remarks>Initiates death without firing IsDead property change events.</remarks>
        /// </summary>
        public void HandleRefitReplacementCompleted() {
            _isDead = true; // avoids firing any IsDead property change handlers
            IsOperational = false;  // we want these property change handlers
            DeactivateAllCmdModuleEquipment();
        }

        protected override void DeactivateAllCmdModuleEquipment() {
            base.DeactivateAllCmdModuleEquipment();
            Weapons.ForAll(weap => weap.IsActivated = false);
            Sensors.ForAll(sens => sens.IsActivated = false);
            ActiveCountermeasures.ForAll(acm => acm.IsActivated = false);
            ShieldGenerators.ForAll(gen => gen.IsActivated = false);
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            Weapons.ForAll(w => w.isDamagedChanged -= WeaponIsDamagedChangedEventHandler);
            Sensors.ForAll(s => s.isDamagedChanged -= SensorIsDamagedChangedEventHandler);
            ActiveCountermeasures.ForAll(cm => cm.isDamagedChanged -= CountermeasureIsDamagedChangedEventHandler);
            ShieldGenerators.ForAll(gen => gen.isDamagedChanged -= ShieldGeneratorIsDamagedChangedEventHandler);
        }

        #region Nested Classes

        [Obsolete]
        public class EquipmentDamagedFromRefit {

            public IEnumerable<PassiveCountermeasure> PassiveCMs { get; set; }
            public IEnumerable<ActiveCountermeasure> ActiveCMs { get; set; }
            public IEnumerable<ShieldGenerator> ShieldGenerators { get; set; }
            public IEnumerable<AWeapon> Weapons { get; set; }
            public IEnumerable<ElementSensor> Sensors { get; set; }
            public Engine FtlEngine { get; set; }

            public EquipmentDamagedFromRefit() { }

            public void RestoreUndamagedState() {
                PassiveCMs.ForAll(cm => cm.IsDamaged = false);
                ActiveCMs.ForAll(cm => cm.IsDamaged = false);
                ShieldGenerators.ForAll(sg => sg.IsDamaged = false);
                Weapons.ForAll(w => w.IsDamaged = false);
                Sensors.ForAll(s => s.IsDamaged = false);
                if (FtlEngine != null) {
                    FtlEngine.IsDamaged = false;
                }
            }
        }

        public class EquipmentDamagedFromRework {

            public IEnumerable<PassiveCountermeasure> PassiveCMs { get; set; }
            public IEnumerable<ActiveCountermeasure> ActiveCMs { get; set; }
            public IEnumerable<ShieldGenerator> ShieldGenerators { get; set; }
            public IEnumerable<AWeapon> Weapons { get; set; }
            public IEnumerable<ElementSensor> Sensors { get; set; }
            public Engine FtlEngine { get; set; }

            public EquipmentDamagedFromRework() { }

            public void RestoreUndamagedState() {
                PassiveCMs.ForAll(cm => cm.IsDamaged = false);
                ActiveCMs.ForAll(cm => cm.IsDamaged = false);
                ShieldGenerators.ForAll(sg => sg.IsDamaged = false);
                Weapons.ForAll(w => w.IsDamaged = false);
                Sensors.ForAll(s => s.IsDamaged = false);
                if (FtlEngine != null) {
                    FtlEngine.IsDamaged = false;
                }
            }
        }

        public class PreReworkValuesStorage {

            public bool WasUsedToRestorePreReworkValues { get; set; }

            public AUnitElementDesign Design { get; private set; }

            // No need for Outputs as it can be regenerated by Data from equipment

            public float CurrentHitPts { get; private set; }

            public EquipmentDamagedFromRework EquipmentDamaged { get; set; }

            public PreReworkValuesStorage(AUnitElementDesign design, float currentHitPts) {
                Design = design;
                CurrentHitPts = currentHitPts;
            }
        }

        public class WeaponsRangeChangingEventArgs : EventArgs {

            public RangeDistance IncomingWeaponsRange { get; private set; }

            public WeaponsRangeChangingEventArgs(RangeDistance incomingWeaponsRange) {
                IncomingWeaponsRange = incomingWeaponsRange;
            }
        }

        #endregion

    }
}

