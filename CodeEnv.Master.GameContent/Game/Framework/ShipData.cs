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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Data associated with a ShipItem.
    /// </summary>
    public class ShipData : AUnitElementData {

        #region FTL

        // Assume for now that all ships are FTL capable. In the future, I will want some defensive ships to be limited to System space.
        // IMPROVE will need to replace this with FtlShipData-derived class as non-FTL ships won't be part of fleets, aka FormationStation, etc
        // public bool IsShipFtlCapable { get { return true; } }

        private bool _isFtlOperational;
        /// <summary>
        /// Indicates whether the FTL engines are operational, aka activated, undamaged and not damped by an FTL damping field.
        /// </summary>
        public bool IsFtlOperational {
            get { return _isFtlOperational; }
            private set { SetProperty<bool>(ref _isFtlOperational, value, "IsFtlOperational", IsFtlOperationalPropChangedHandler); }
        }

        private bool _isFtlDamaged;
        /// <summary>
        /// Indicates whether the FTL engines are damaged. 
        /// </summary>
        public bool IsFtlDamaged {
            get { return _isFtlDamaged; }
            set { SetProperty<bool>(ref _isFtlDamaged, value, "IsFtlDamaged", IsFtlDamagedPropChangedHandler); }
        }

        private bool _isFtlActivated;
        /// <summary>
        /// Indicates whether the FTL engines are activated. 
        /// </summary>
        public bool IsFtlActivated {
            get { return _isFtlActivated; }
            set { SetProperty<bool>(ref _isFtlActivated, value, "IsFtlActivated", IsFtlActivatedPropChangedHandler); }
        }

        private bool _isFtlDampedByField;
        /// <summary>
        /// Indicates whether the FTL engines are damped by an FTL Damping Field. 
        /// </summary>
        public bool IsFtlDampedByField {
            get { return _isFtlDampedByField; }
            set { SetProperty<bool>(ref _isFtlDampedByField, value, "IsFtlDampedByField", IsFtlDampedByFieldPropChangedHandler); }
        }

        #endregion

        private INavigable _target;
        public INavigable Target {
            get { return _target; }
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
                SetProperty<INavigable>(ref _target, value, "Target");
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
                D.Assert(_currentDrag != Constants.ZeroF);
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
        public float FullPropulsionPower { get { return IsFtlOperational ? FullFtlPropulsionPower : FullStlPropulsionPower; } }

        /// <summary>
        /// The maximum power that can be projected by the STL engines.         
        /// <remarks>See Flight.txt for equations.</remarks>
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in realtime to a Unity seconds value 
        /// in the EngineRoom using GameTime.GameSpeedAdjustedHoursPerSecond.
        /// </summary>
        public float FullStlPropulsionPower { get { return _enginesStat.FullStlPropulsionPower; } }

        /// <summary>
        /// The maximum power that can be projected by the FTL engines.
        /// <remarks>See Flight.txt for equations.</remarks>
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in realtime to a Unity seconds value 
        /// in the EngineRoom using GameTime.GameSpeedAdjustedHoursPerSecond.
        /// </summary>
        public float FullFtlPropulsionPower { get { return _enginesStat.FullFtlPropulsionPower; } }

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
            private set { SetProperty<float>(ref _fullSpeedValue, value, "FullSpeedValue"); }
        }

        /// <summary>
        /// The maximum turn rate of the ship in degrees per hour.
        /// </summary>
        public float MaxTurnRate { get { return _enginesStat.MaxTurnRate; } }

        public override IntVector3 SectorID { get { return References.SectorGrid.GetSectorIdThatContains(Position); } }

        public new ShipInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as ShipInfoAccessController; } }

        protected new IShip Item { get { return base.Item as IShip; } }

        private new ShipHullEquipment HullEquipment { get { return base.HullEquipment as ShipHullEquipment; } }

        private EnginesStat _enginesStat;
        private GameTime _gameTime;

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipData" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        /// <param name="hqPriority">The HQ priority.</param>
        /// <param name="enginesStat">The engines stat.</param>
        /// <param name="combatStance">The combat stance.</param>
        public ShipData(IShip ship, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, ShipHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            Priority hqPriority, EnginesStat enginesStat, ShipCombatStance combatStance)
            : base(ship, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority) {
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;

            _enginesStat = enginesStat;
            CombatStance = combatStance;
            InitializeLocalValuesAndReferences();
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
            //D.Log(ShowDebugLog, "{0}.CommenceOperations() setting Topography to {1}.", FullName, Topography.GetValueName());
            IsFtlActivated = true;  // will trigger Data.AssessIsFtlOperational()
        }

        /// <summary>
        /// Returns the capacity for repair available to this ship in the RepairMode provided.
        /// UOM is hitPts per day. IMPROVE Acquire value from data fields.
        /// </summary>
        /// <param name="mode">The RepairMode.</param>
        /// <returns></returns>
        public float GetRepairCapacity(RepairMode mode) {
            switch (mode) {
                case RepairMode.Self:
                    return 2F;    // HACK
                case RepairMode.PlanetHighOrbit:
                    return 4F;    // HACK
                case RepairMode.PlanetCloseOrbit:
                    return 6F;    // HACK
                case RepairMode.AlliedPlanetHighOrbit:
                    return 8F;    // HACK
                case RepairMode.AlliedPlanetCloseOrbit:
                    return 10F;    // HACK
                case RepairMode.BaseHighOrbit:
                case RepairMode.BaseCloseOrbit:
                case RepairMode.AlliedBaseHighOrbit:
                case RepairMode.AlliedBaseCloseOrbit:
                case RepairMode.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
        }

        #region Event and Property Change Handlers

        private void IsFtlOperationalPropChangedHandler() {
            HandleIsFtlOperationalChanged();
        }

        private void HandleIsFtlOperationalChanged() {
            string msg = IsFtlOperational ? "now" : "no longer";
            D.Log(ShowDebugLog, "{0} FTL is {1} operational.", FullName, msg);
            RefreshFullSpeedValue();
        }

        private void IsFtlDampedByFieldPropChangedHandler() {
            AssessIsFtlOperational();
        }

        private void IsFtlDamagedPropChangedHandler() {
            AssessIsFtlOperational();
        }

        private void IsFtlActivatedPropChangedHandler() {
            AssessIsFtlOperational();
        }

        private void CurrentDragPropChangedHandler() {
            RefreshFullSpeedValue();
        }

        protected override void HandleTopographyChanged() {
            base.HandleTopographyChanged();
            CurrentDrag = OpenSpaceDrag * Topography.GetRelativeDensity();
        }

        #endregion

        /// <summary>
        /// Refreshes the full speed value the ship is capable of achieving.
        /// </summary>
        private void RefreshFullSpeedValue() {
            FullSpeedValue = GameUtility.CalculateMaxAttainableSpeed(FullPropulsionPower, Mass, CurrentDrag);
        }

        private void AssessIsFtlOperational() {
            IsFtlOperational = IsFtlActivated && !IsFtlDamaged && !IsFtlDampedByField;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        public enum RepairMode {

            None,

            /// <summary>
            /// Repairing executed in a slot of the Formation
            /// using facilities and materials available on the ship.
            /// </summary>
            Self,

            /// <summary>
            /// Repairing executed in high orbit around a non-allied planet
            /// using facilities available on the ship and materials that can
            /// be acquired (LR) from the planet.
            /// </summary>
            PlanetHighOrbit,

            /// <summary>
            /// Repairing executed in high orbit around an allied planet
            /// using facilities available on the planet and ship, and materials that can
            /// be delivered (LR) by the inhabitants of the planet.
            /// </summary>
            AlliedPlanetHighOrbit,

            /// <summary>
            /// Repairing executed in close orbit around a non-allied planet
            /// using facilities available on the ship and materials that can
            /// be acquired (SR) from the planet.
            /// </summary>
            PlanetCloseOrbit,

            /// <summary>
            /// Repairing executed in close orbit around an allied planet
            /// using facilities available on the planet and ship, and materials that can
            /// be delivered (SR) by the inhabitants of the planet.
            /// </summary>
            AlliedPlanetCloseOrbit,

            /// <summary>
            /// Repairing executed in high orbit around a non-allied Settlement or Starbase
            /// using facilities available on the base and ship and materials that can
            /// be delivered (LR) by the inhabitants of the base.
            /// </summary>
            BaseHighOrbit,

            /// <summary>
            /// Repairing executed in close orbit around a non-allied Settlement or Starbase
            /// using facilities available on the base and ship and materials that can
            /// be delivered (SR) by the inhabitants of the base.
            /// </summary>
            BaseCloseOrbit,

            /// <summary>
            /// Repairing executed in high orbit around an allied Settlement or Starbase
            /// using facilities available on the base and ship and materials that can
            /// be delivered (LR) by the inhabitants of the base.
            /// </summary>
            AlliedBaseHighOrbit,

            /// <summary>
            /// Repairing executed in close orbit around an allied Settlement or Starbase
            /// using facilities available on the base and ship and materials that can
            /// be delivered (SR) by the inhabitants of the base.
            /// </summary>
            AlliedBaseCloseOrbit

        }

        #endregion

    }
}

