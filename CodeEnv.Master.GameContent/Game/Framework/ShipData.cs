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

        #endregion

        private INavigableDestination _target;
        public INavigableDestination Target {
            get { return _target; }
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
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
                if (value > TempGameValues.__ShipMaxSpeedValue) {
                    D.Warn("{0}.FullSpeedValue {1:0.000000} > MaxSpeedValue {2:0.##}. Correcting.", DebugName, value, TempGameValues.__ShipMaxSpeedValue);
                    value = TempGameValues.__ShipMaxSpeedValue;
                }
                SetProperty<float>(ref _fullSpeedValue, value, "FullSpeedValue");
            }
        }

        /// <summary>
        /// The maximum turn rate of the ship in degrees per hour.
        /// </summary>
        public float MaxTurnRate { get { return IsFtlOperational ? _ftlEngine.MaxTurnRate : _stlEngine.MaxTurnRate; } }

        public override IntVector3 SectorID { get { return GameReferences.SectorGrid.GetSectorIDThatContains(Position); } }

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
            Priority hqPriority, Engine stlEngine, ShipCombatStance combatStance, float constructionCost, string designName)
            : this(ship, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority, stlEngine, null,
                  combatStance, constructionCost, designName) {
        }

        public ShipData(IShip ship, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs, ShipHullEquipment hullEquipment,
            IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<ElementSensor> sensors, IEnumerable<ShieldGenerator> shieldGenerators,
            Priority hqPriority, Engine stlEngine, FtlEngine ftlEngine, ShipCombatStance combatStance, float constructionCost, string designName)
            : base(ship, owner, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators, hqPriority, constructionCost, designName) {
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;

            _stlEngine = stlEngine;
            _ftlEngine = ftlEngine;
            if (ftlEngine != null) {
                ftlEngine.isOperationalChanged += IsFtlOperationalChangedEventHandler;
            }
            CombatStance = combatStance;
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
            //D.Log(ShowDebugLog, "{0}.CommenceOperations() setting Topography to {1}.", DebugName, Topography.GetValueName());
            _stlEngine.IsActivated = true;
            if (_ftlEngine != null) {
                _ftlEngine.IsActivated = true;
            }
            RefreshFullSpeedValue();
        }

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

    }
}

