﻿// --------------------------------------------------------------------------------------------------------------------
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

        /// <summary>
        /// Returns <c>true</c> if this element has weapons and one or more of them are undamaged, <c>false</c> otherwise.
        /// <remarks>Whatever the response, it does not imply anything about the operational state of the weapon(s) as 
        /// having operational weapons requires 1) the presence of one or more weapons, 2) one or more weapons being undamaged, and 3) the weapons being activated.</remarks>
        /// </summary>
        public bool HasUndamagedWeapons { get { return Weapons.Where(w => !w.IsDamaged).Any(); } }

        public IList<AWeapon> Weapons { get { return HullEquipment.Weapons; } }
        public IList<ElementSensor> Sensors { get; private set; }
        public IList<ActiveCountermeasure> ActiveCountermeasures { get; private set; }
        public IList<ShieldGenerator> ShieldGenerators { get; private set; }

        public Vector3 HullDimensions { get { return HullEquipment.HullDimensions; } }

        public override string DebugName {
            get {
                string ownerName = Owner != null ? Owner.DebugName : Constants.Empty;   // can be null during initialization
                return DebugNameFormat.Inject(ownerName, UnitName, Name);
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
            protected set { SetProperty<OutputsYield>(ref _outputs, value, "Outputs"); }
        }

        /// <summary>
        /// The current design of this Element.
        /// </summary>
        public AUnitElementDesign Design { get; private set; }

        /// <summary>
        /// The mass of this Element.
        /// <remarks>7.26.16 An element's interaction with the physics engine uses the mass from its 
        /// Rigidbody which is set one time in the UnitFactory using this value.</remarks>
        /// </summary>
        public float Mass { get; protected set; }

        public string UnitName { get { return CmdData != null ? CmdData.UnitName : "No Unit"; } }

        public int UnitElementCount { get { return CmdData != null ? CmdData.ElementCount : Constants.Zero; } }

        internal AUnitCmdData CmdData { private get; set; }

        protected sealed override IntelCoverage DefaultStartingIntelCoverage { get { return IntelCoverage.None; } }

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
            : base(element, owner, design.HitPoints, passiveCMs) {
            HullEquipment = hullEquipment;
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

        /// <summary>
        /// Calculates and returns the mass of this element.
        /// <remarks>Must be called from derived class constructor after 
        /// all Equipment that contributes to the element's total mass has been assigned.</remarks>
        /// </summary>
        /// <returns></returns>
        protected virtual float CalculateMass() {
            return HullEquipment.Mass + Weapons.Sum(w => w.Mass) + ActiveCountermeasures.Sum(cm => cm.Mass) + Sensors.Sum(s => s.Mass)
                + PassiveCountermeasures.Sum(cm => cm.Mass) + ShieldGenerators.Sum(gen => gen.Mass);
        }

        #endregion

        public override void CommenceOperations() {
            base.CommenceOperations();
            D.AssertNotEqual(Constants.ZeroF, Mass, "Check derived class call to CalculateMass() from its constructor.");
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
        /// Simulates Rework underway by damaging non-hull equipment including CMs, ShieldGenerators, Weapons and Sensors. 
        /// The equipment damaged is determined by 1) whether its damageable, 2) how many of each type are kept 
        /// undamaged to simulate a minimal defense while being reworked, and 3) the damagePercent.
        /// <remarks>This approach allows a degree of control on the availability of equipment
        /// as rework proceeds and doesn't interfere with use of operational equipment when AlertLevel changes.</remarks>
        /// </summary>
        /// <param name="damagePercent">The damage percent.</param>
        protected virtual EquipmentDamagedFromRework DamageNonHullEquipment(float damagePercent) {
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

        protected override void RemoveDamageFromAllEquipment() {
            base.RemoveDamageFromAllEquipment();
            ActiveCountermeasures.Where(cm => cm.IsDamageable).ForAll(cm => cm.IsDamaged = false);
            ShieldGenerators.Where(gen => gen.IsDamageable).ForAll(gen => gen.IsDamaged = false);
            Weapons.Where(w => w.IsDamageable).ForAll(w => w.IsDamaged = false);
            Sensors.Where(s => s.IsDamageable).ForAll(s => s.IsDamaged = false);
        }

        #endregion

        #region Combat Support

        protected override float AssessDamageToEquipment(float damageSeverity) {
            float cumCurrentHitPtReductionFromEquip = base.AssessDamageToEquipment(damageSeverity);
            var damageChance = damageSeverity;

            var undamagedDamageableWeapons = Weapons.Where(w => w.IsDamageable && !w.IsDamaged);
            foreach (var w in undamagedDamageableWeapons) {
                bool toDamage = RandomExtended.Chance(damageChance);
                if (toDamage) {
                    w.IsDamaged = true;
                    cumCurrentHitPtReductionFromEquip += w.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been damaged.", DebugName, w.Name);
                }
            }

            var undamagedDamageableSensors = Sensors.Where(s => s.IsDamageable && !s.IsDamaged);
            foreach (var s in undamagedDamageableSensors) {
                bool toDamage = RandomExtended.Chance(damageChance);
                if (toDamage) {
                    s.IsDamaged = true;
                    cumCurrentHitPtReductionFromEquip += s.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been damaged.", DebugName, s.Name);
                }
            }

            var undamagedDamageableActiveCMs = ActiveCountermeasures.Where(cm => cm.IsDamageable && !cm.IsDamaged);
            foreach (var aCM in undamagedDamageableActiveCMs) {
                bool toDamage = RandomExtended.Chance(damageChance);
                if (toDamage) {
                    aCM.IsDamaged = true;
                    cumCurrentHitPtReductionFromEquip += aCM.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been damaged.", DebugName, aCM.Name);
                }
            }

            var undamagedDamageableGenerators = ShieldGenerators.Where(gen => gen.IsDamageable && !gen.IsDamaged);
            foreach (var g in undamagedDamageableGenerators) {
                bool toDamage = RandomExtended.Chance(damageChance);
                if (toDamage) {
                    g.IsDamaged = true;
                    cumCurrentHitPtReductionFromEquip += g.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been damaged.", DebugName, g.Name);
                }
            }
            return cumCurrentHitPtReductionFromEquip;
        }

        #endregion

        #region Repair

        protected override float AssessRepairToEquipment(float repairImpact) {
            float cumRprPtsFromEquip = base.AssessRepairToEquipment(repairImpact);

            var rprChance = repairImpact;

            var damagedWeapons = Weapons.Where(w => w.IsDamaged);
            foreach (var w in damagedWeapons) {
                bool toRpr = RandomExtended.Chance(rprChance);
                if (toRpr) {
                    w.IsDamaged = false;
                    cumRprPtsFromEquip += w.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been repaired.", DebugName, w.Name);
                }
            }

            var damagedSensors = Sensors.Where(s => s.IsDamaged);
            foreach (var s in damagedSensors) {
                bool toRpr = RandomExtended.Chance(rprChance);
                if (toRpr) {
                    s.IsDamaged = false;
                    cumRprPtsFromEquip += s.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been repaired.", DebugName, s.Name);
                }
            }

            var damagedActiveCMs = ActiveCountermeasures.Where(cm => cm.IsDamaged);
            foreach (var aCM in damagedActiveCMs) {
                bool toRpr = RandomExtended.Chance(rprChance);
                if (toRpr) {
                    aCM.IsDamaged = false;
                    cumRprPtsFromEquip += aCM.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been repaired.", DebugName, aCM.Name);
                }
            }

            var damagedGenerators = ShieldGenerators.Where(gen => gen.IsDamaged);
            foreach (var g in damagedGenerators) {
                bool toRpr = RandomExtended.Chance(rprChance);
                if (toRpr) {
                    g.IsDamaged = true;
                    cumRprPtsFromEquip += g.HitPoints;
                    D.Log(ShowDebugLog, "{0}'s {1} has been repaired.", DebugName, g.Name);
                }
            }

            return cumRprPtsFromEquip;
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
            var cmDamageMitigation = allCountermeasures.Where(cm => !cm.IsDamaged).Select(cm => cm.DmgMitigation).Aggregate(default(DamageStrength), (accum, cmDmgMit) => accum + cmDmgMit);
            DamageMitigation = HullEquipment.DamageMitigation + cmDamageMitigation;
            DefensiveStrength = new CombatStrength(allCountermeasures, HullEquipment.DamageMitigation);
        }

        private void RecalcOffensiveStrength() {
            OffensiveStrength = new CombatStrength(Weapons);
        }

        protected override void HandleOwnerChanging(Player incomingOwner) {
            base.HandleOwnerChanging(incomingOwner);
            AssessChangeToCmdOwner(incomingOwner);
        }

        private void AssessChangeToCmdOwner(Player incomingOwner) {
            if (CmdData.ElementCount == Constants.One) {
                D.AssertEqual(CmdData.ElementsData.First(), this);
                // 6.22.18 This owner change must be propagated to Command in this changing phase as AUnitElementItem will 
                // follow with changes during the same changing phase. These changes will be to old owner's and allies
                // IntelCoverage of the element. The changing phase must be used so that the right PlayerKnowledge and allies are used
                // since once Owner changes they will be different. 
                // If Cmd's Owner is not changed now, AUnitElementItem's changes will generate errors in PlayerKnowledge. 
                // Cmd's PlayerKnowledge will still be that of the 'about to be changed' owner and it 
                // will throw an error when it finds that its owned element doesn't have IntelCoverage.Comprehensive. 
                // The same thing will happen when it finds ally coverage of the element isn't Comprehensive.
                D.LogBold("{0} is about to change {1}'s Owner to {2}.", DebugName, CmdData.DebugName, incomingOwner.DebugName);
                CmdData.Owner = incomingOwner;
            }
        }

        protected sealed override void PropagateOwnerChange() { // 6.22.18 see AssessChangeToCmdOwner above
            base.PropagateOwnerChange();    // base does nothing
        }

        /// <summary>
        /// Called on the element that was refitted and therefore replaced.
        /// <remarks>Initiates death without firing IsDead property change events.</remarks>
        /// </summary>
        public void HandleRefitReplacementCompleted() {
            _isDead = true; // avoids firing any IsDead property change handlers
            IsOperational = false;  // we want these property change handlers
            DeactivateAllEquipment();
        }

        protected override void DeactivateAllEquipment() {
            base.DeactivateAllEquipment();
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

        #region Debug

        protected override void __ValidateAllEquipmentDamageRepaired() {
            base.__ValidateAllEquipmentDamageRepaired();
            Weapons.ForAll(w => D.Assert(!w.IsDamaged));
            Sensors.ForAll(s => D.Assert(!s.IsDamaged));
            ActiveCountermeasures.ForAll(cm => D.Assert(!cm.IsDamaged));
            ShieldGenerators.ForAll(gen => D.Assert(!gen.IsDamaged));
        }

        #endregion

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

