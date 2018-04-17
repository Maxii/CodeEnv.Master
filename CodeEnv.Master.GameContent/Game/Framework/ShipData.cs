// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipData.cs
// Data associated with a ShipItem.
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
    /// Data associated with a ShipItem.
    /// </summary>
    public class ShipData : AUnitElementData {

        #region FTL

        /// <summary>
        /// Indicates whether this ship has FTL capability. If <c>true</c> it does not imply
        /// that capability is currently operational. If <c>false</c> the ship does not have an FTL engine installed.
        /// </summary>
        public bool IsFtlCapable { get { return _ftlEngine != null; } }

        /// <summary>
        /// Indicates whether the FTL engines are operational, aka activated, undamaged and not damped by an FTL damping field.
        /// <remarks>Test IsFtlCapable before using this property as if not, this property will always return false.</remarks>
        /// </summary>
        public bool IsFtlOperational { get { return _ftlEngine != null && _ftlEngine.IsOperational; } }

        /// <summary>
        /// Indicates whether the FTL engines are damaged. 
        /// <remarks>Test IsFtlCapable before using this property as if not, this property will always return false.</remarks>
        /// </summary>
        public bool IsFtlDamaged {
            get { return _ftlEngine != null && _ftlEngine.IsDamaged; }
            set {
                if (_ftlEngine == null) {
                    D.Warn("{0}: Attempting to change the damage state of an FtlEngine that is not present.", DebugName);
                    return;
                }
                _ftlEngine.IsDamaged = value;
            }
        }

        /// <summary>
        /// Indicates whether the FTL engines are damped by an FTL Damping Field. 
        /// <remarks>Test IsFtlCapable before using this property as if not, this property will always return false.</remarks>
        /// </summary>
        public bool IsFtlDampedByField {
            get { return _ftlEngine != null && _ftlEngine.IsDampedByField; }
            set {
                if (_ftlEngine == null) {
                    D.Warn("{0}: Attempting to change the damped state of an FtlEngine that is not present.", DebugName);
                    return;
                }
                _ftlEngine.IsDampedByField = value;
            }
        }

        ////public float FtlEngineHitPoints { get { return _ftlEngine != null ? _ftlEngine.MaxHitPoints : Constants.ZeroF; } }

        #endregion

        private INavigableDestination _target;
        public INavigableDestination Target {
            get { return _target; }
            set {
                if (_target != null && _target.Equals(value)) { return; }   // OPTIMIZE eliminates equality warning. 1.1.18 using == allowed a warning
                SetProperty<INavigableDestination>(ref _target, value, "Target");
            }
        }

        public ShipHullCategory HullCategory { get { return HullEquipment.HullCategory; } }

        private ShipCombatStance _combatStance;
        public ShipCombatStance CombatStance {
            get { return _combatStance; }
            set { SetProperty<ShipCombatStance>(ref _combatStance, value, "CombatStance"); }
        }

        /// <summary>
        /// Read-only. The actual speed of the ship in Units per hour. Whether paused or at a GameSpeed
        /// other than Normal (x1), this property always returns the proper reportable value.
        /// </summary>
        public float ActualSpeedValue { get { return Item.ActualSpeedValue; } }

        /// <summary>
        /// The current speed setting of this ship.
        /// </summary>
        private Speed _currentSpeedSetting;
        public Speed CurrentSpeedSetting {
            get { return _currentSpeedSetting; }
            set { SetProperty<Speed>(ref _currentSpeedSetting, value, "CurrentSpeedSetting"); }
        }

        /// <summary>
        /// Read-only. The real-time, normalized heading of the ship in worldspace coordinates. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading { get { return Item.CurrentHeading; } }

        private float _currentDrag;
        /// <summary>
        /// The drag of the ship in its current Topography.
        /// </summary>
        public float CurrentDrag {
            get {
                D.AssertNotDefault(_currentDrag);
                return _currentDrag;
            }
            private set { SetProperty<float>(ref _currentDrag, value, "CurrentDrag", CurrentDragPropChangedHandler); }
        }

        /// <summary>
        /// The drag of the ship in Topography.OpenSpace.
        /// </summary>
        public float OpenSpaceDrag { get { return HullEquipment.Drag; } }

        /// <summary>
        /// The maximum power that can currently be projected by the engines. 
        /// <remarks>See Flight.txt for equations.</remarks>
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in realtime to a Unity seconds value 
        /// in the EngineRoom using GameTime.GameSpeedAdjustedHoursPerSecond.
        /// </summary>
        public float FullPropulsionPower { get { return IsFtlOperational ? _ftlEngine.FullPropulsionPower : _stlEngine.FullPropulsionPower; } }

        private ShipPublisher _publisher;
        public ShipPublisher Publisher {
            get { return _publisher = _publisher ?? new ShipPublisher(this); }
        }

        private Vector3 _intendedHeading;
        /// <summary>
        /// The ship's normalized requested/intended heading in worldspace coordinates.
        /// </summary>
        public Vector3 IntendedHeading {
            get { return _intendedHeading; }
            set {
                value.ValidateNormalized();
                SetProperty<Vector3>(ref _intendedHeading, value, "IntendedHeading");
            }
        }

        /// <summary>
        /// The maximum speed that the ship can currently achieve in units per hour.
        /// </summary>
        private float _fullSpeedValue;
        public float FullSpeedValue {
            get { return _fullSpeedValue; }
            private set {
                if (value.IsGreaterThan(TempGameValues.__ShipMaxSpeedValue)) {
                    D.Warn("{0}.FullSpeedValue {1:0.000000} > MaxSpeedValue {2:0.##}. Correcting.", DebugName, value, TempGameValues.__ShipMaxSpeedValue);
                    value = TempGameValues.__ShipMaxSpeedValue;
                }
                SetProperty<float>(ref _fullSpeedValue, value, "FullSpeedValue");
            }
        }

        /// <summary>
        /// The turn rate capability of the ship in degrees per hour.
        /// <remarks>4.15.18 Now a function of Engine's MaxTurnRate and the capability of the hull.</remarks>
        /// </summary>
        public float TurnRate {
            get {
                return (IsFtlOperational ? _ftlEngine.MaxTurnRate : _stlEngine.MaxTurnRate) * HullCategory.TurnrateFactor();
            }
        }

        public override IntVector3 SectorID { get { return GameReferences.SectorGrid.GetSectorIDThatContains(Position); } }

        public new ShipDesign Design { get { return base.Design as ShipDesign; } }

        public new ShipInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as ShipInfoAccessController; } }

        protected new IShip Item { get { return base.Item as IShip; } }

        private new ShipHullEquipment HullEquipment { get { return base.HullEquipment as ShipHullEquipment; } }

        /// <summary>
        /// Indicates and controls whether the FTL engines are activated. 
        /// <remarks>Throws an error if no FtlEngine is present, so use IsFtlCapable to test for it.</remarks>
        /// <remarks>Used to deactivate/reactivate the engine when entering/leaving Attacking state.</remarks>
        /// </summary>
        public bool IsFtlActivated {
            get { return _ftlEngine != null && _ftlEngine.IsActivated; }
            set {
                D.AssertNotNull(_ftlEngine);
                _ftlEngine.IsActivated = value;
            }
        }

        private Engine _stlEngine;
        private FtlEngine _ftlEngine;
        private GameTime _gameTime;

        #region Initialization 

        public ShipData(IShip ship, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, ShipHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<ElementSensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            Engine stlEngine, FtlEngine ftlEngine, ShipDesign design)
            : base(ship, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, design) {

            _stlEngine = stlEngine;
            _ftlEngine = ftlEngine;
            if (ftlEngine != null) {
                ftlEngine.isOperationalChanged += IsFtlOperationalChangedEventHandler;
            }
            __ValidateTurnRate();

            Mass = CalculateMass();
            Outputs = MakeOutputs();
            CombatStance = design.CombatStance;
            InitializeLocalValuesAndReferences();
        }

        protected override AIntel MakeIntelInstance() {
            return new RegressibleIntel(lowestRegressedCoverage: IntelCoverage.None);
        }

        private void InitializeLocalValuesAndReferences() {
            _gameTime = GameTime.Instance;
            _intendedHeading = CurrentHeading;  // initialize to something other than Vector3.zero which causes problems with LookRotation
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new ShipInfoAccessController(this);
        }

        public override void FinalInitialize() {
            base.FinalInitialize();
            Topography = _gameMgr.GameKnowledge.GetSpaceTopography(Position);   // will set CurrentDrag
        }

        #endregion

        public override void CommenceOperations() {
            base.CommenceOperations();
            InitializeEngines();
        }

        protected override float CalculateMass() {
            float mass = base.CalculateMass();
            mass += _stlEngine.Mass;
            if (_ftlEngine != null) {
                mass += _ftlEngine.Mass;
            }
            return mass;
        }

        private void InitializeEngines() {
            _stlEngine.CalculatePropulsionPower(Mass, OpenSpaceDrag);
            _stlEngine.IsActivated = true;
            if (_ftlEngine != null) {
                _ftlEngine.CalculatePropulsionPower(Mass, OpenSpaceDrag);
                _ftlEngine.IsActivated = true;
            }
            RefreshFullSpeedValue();
        }

        public ShipReport GetReport(Player player) { return Publisher.GetReport(player); }

        #region Event and Property Change Handlers

        private void IsFtlOperationalChangedEventHandler(object sender, EventArgs e) {
            HandleIsFtlOperationalChanged();
        }

        private void HandleIsFtlOperationalChanged() {
            //D.Log(ShowDebugLog, "{0} FTL is {1} operational.", DebugName, IsFtlOperational ? "now" : "no longer");
            RefreshFullSpeedValue();
        }

        private void CurrentDragPropChangedHandler() {
            RefreshFullSpeedValue();
        }

        protected override void HandleTopographyChanged() {
            base.HandleTopographyChanged();
            CurrentDrag = OpenSpaceDrag * Topography.GetRelativeDensity();
        }

        #endregion

        public override void RestoreInitialConstructionValues() {
            base.RestoreInitialConstructionValues();
            Outputs = MakeOutputs();
        }

        public override void RestorePreReworkValues(PreReworkValuesStorage valuesBeforeRework) {
            base.RestorePreReworkValues(valuesBeforeRework);
            Outputs = MakeOutputs();
        }

        protected override EquipmentDamagedFromRework DamageNonHullEquipment(float damagePercent) {
            var equipmentDamagedFromRework = base.DamageNonHullEquipment(damagePercent);
            if (_ftlEngine != null) {
                _ftlEngine.IsDamaged = RandomExtended.Chance(damagePercent);
                if (_ftlEngine.IsDamaged) {
                    equipmentDamagedFromRework.FtlEngine = _ftlEngine;
                }
            }
            // StlEngine is not damageable
            return equipmentDamagedFromRework;
        }

        protected override void RemoveDamageFromAllEquipment() {
            base.RemoveDamageFromAllEquipment();
            if (_ftlEngine != null) {
                _ftlEngine.IsDamaged = false;
            }
            // StlEngine is not damageable
        }

        protected override void DeactivateAllEquipment() {
            base.DeactivateAllEquipment();
            _stlEngine.IsActivated = false;
            if (_ftlEngine != null) {
                _ftlEngine.IsActivated = false;
            }
        }

        #region Combat Support

        protected override float AssessDamageToEquipment(float damageSeverity) {
            float cumCurrentHitPtReductionFromEquip = base.AssessDamageToEquipment(damageSeverity);
            if (_ftlEngine != null) {
                if (!_ftlEngine.IsDamaged) {
                    var dmgChance = damageSeverity;
                    bool toDamage = RandomExtended.Chance(dmgChance);
                    if (toDamage) {
                        _ftlEngine.IsDamaged = true;
                        cumCurrentHitPtReductionFromEquip += _ftlEngine.HitPoints;
                    }
                }
            }
            // StlEngine is not damageable
            return cumCurrentHitPtReductionFromEquip;
        }

        #endregion

        #region Repair

        protected override float AssessRepairToEquipment(float repairImpact) {
            float cumEquipRprPts = base.AssessRepairToEquipment(repairImpact);
            if (_ftlEngine != null) {
                if (_ftlEngine.IsDamaged) {
                    float rprChance = repairImpact;
                    bool toRpr = RandomExtended.Chance(rprChance);
                    if (toRpr) {
                        _ftlEngine.IsDamaged = false;
                        cumEquipRprPts += _ftlEngine.HitPoints;
                    }
                }
            }
            // StlEngine is not damageable
            return cumEquipRprPts;
        }

        #endregion

        private OutputsYield MakeOutputs() {
            IList<OutputsYield.OutputValuePair> outputPairs = new List<OutputsYield.OutputValuePair>(7);

            float nonHullExpense = HullEquipment.Weapons.Sum(w => w.Expense) + PassiveCountermeasures.Sum(pcm => pcm.Expense)
                + ActiveCountermeasures.Sum(acm => acm.Expense) + Sensors.Sum(s => s.Expense) + ShieldGenerators.Sum(gen => gen.Expense)
                + _stlEngine.Expense;
            nonHullExpense += _ftlEngine != null ? _ftlEngine.Expense : Constants.ZeroF;

            var allOutputIDs = Enums<OutputID>.GetValues(excludeDefault: true);
            foreach (var id in allOutputIDs) {
                float yield;
                if (HullEquipment.TryGetYield(id, out yield)) {
                    if (id == OutputID.Expense) {
                        yield += nonHullExpense;
                    }
                    else if (id == OutputID.NetIncome) {
                        yield -= nonHullExpense;
                    }
                    outputPairs.Add(new OutputsYield.OutputValuePair(id, yield));
                }
            }
            return new OutputsYield(outputPairs.ToArray());
        }

        /// <summary>
        /// Refreshes the full speed value the ship is capable of achieving.
        /// </summary>
        private void RefreshFullSpeedValue() {
            FullSpeedValue = GameUtility.CalculateMaxAttainableSpeed(FullPropulsionPower, Mass, CurrentDrag);
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            if (_ftlEngine != null) {
                _ftlEngine.isOperationalChanged -= IsFtlOperationalChangedEventHandler;
            }
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateTurnRate() {
            float stlTurnRate = _stlEngine.MaxTurnRate * HullCategory.TurnrateFactor();
            if (stlTurnRate.IsLessThan(TempGameValues.MinimumTurnRate)) {
                D.Warn("{0}'s STL TurnRate {1:0.#} is too low. Game MinTurnRate = {2:0.#}.", DebugName, stlTurnRate, TempGameValues.MinimumTurnRate);
            }
            if (_ftlEngine != null) {
                float ftlTurnRate = _ftlEngine.MaxTurnRate * HullCategory.TurnrateFactor();
                if (ftlTurnRate.IsLessThan(TempGameValues.MinimumTurnRate)) {
                    D.Warn("{0}'s FTL TurnRate {1:0.#} is too low. Game MinTurnRate = {2:0.#}.", DebugName, stlTurnRate, TempGameValues.MinimumTurnRate);
                }
            }
        }

        protected override void __ValidateAllEquipmentDamageRepaired() {
            base.__ValidateAllEquipmentDamageRepaired();
            if (_ftlEngine != null) {
                D.Assert(!_ftlEngine.IsDamaged);
            }
        }

        #endregion

    }
}

