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
    using UnityEngine;

    /// <summary>
    /// Data associated with a ShipItem.
    /// </summary>
    public class ShipData : AUnitElementItemData {

        #region FTL

        // Assume for now that all ships are FTL capable. In the future, I will want some defensive ships to be limited to System space.
        // IMPROVE will need to replace this with FtlShipData-derived class as non-Ftl ships won't be part of fleets, aka FormationStation, etc
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

        private INavigableTarget _target;
        public INavigableTarget Target {
            get { return _target; }
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
                SetProperty<INavigableTarget>(ref _target, value, "Target");
            }
        }

        public ShipHullCategory HullCategory { get { return HullEquipment.HullCategory; } }

        private ShipCombatStance _combatStance; //TODO not currently used
        public ShipCombatStance CombatStance {
            get { return _combatStance; }
            set { SetProperty<ShipCombatStance>(ref _combatStance, value, "CombatStance"); }
        }

        /// <summary>
        /// Readonly. Gets the current speed of the ship in Units per hour. Whether paused or at a GameSpeed
        /// other than Normal (x1), this property always returns the value assuming not paused with GameSpeed.Normal.
        /// </summary>
        public float CurrentSpeedValue {
            get {
                if (_gameMgr.IsPaused) {
                    return _currentSpeedValueOnPause;
                }
                else {
                    var speedInGameSpeedAdjustedUnitsPerSec = _shipRigidbody.velocity.magnitude;
                    var speedInUnitsPerHour = speedInGameSpeedAdjustedUnitsPerSec / _gameTime.GameSpeedAdjustedHoursPerSecond;
                    return speedInUnitsPerHour;
                }
            }
        }

        private float _requestedSpeedValue;
        /// <summary>
        /// The desired speed this ship should be traveling at in Units per hour. 
        /// The thrust of the ship will be adjusted to accelerate or decelerate to this speed.
        /// </summary>
        public float RequestedSpeedValue {
            get { return _requestedSpeedValue; }
            set {
                D.Warn(value > FullSpeedValue, "{0} RequestedSpeedValue {1:0.0000} > FullSpeedValue {2:0.0000}.", FullName, value, FullSpeedValue);
                SetProperty<float>(ref _requestedSpeedValue, value, "RequestedSpeedValue");
            }
        }

        private Speed _requestedSpeed;
        /// <summary>
        /// The desired speed setting this ship should be traveling at. 
        /// </summary>
        public Speed RequestedSpeed {
            get { return _requestedSpeed; }
            set { SetProperty<Speed>(ref _requestedSpeed, value, "RequestedSpeed"); }
        }

        /// <summary>
        /// The drag of the ship in Topography.OpenSpace.
        /// </summary>
        public float Drag { get { return HullEquipment.Drag; } }

        /// <summary>
        /// The maximum power that can currently be projected by the engines. FullSpeed = FullPropulsionPower / (Mass * _rigidbody.drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in realtime to a Unity seconds value 
        /// in the EngineRoom using GameTime.GameSpeedAdjustedHoursPerSecond.
        /// </summary>
        public float FullPropulsionPower { get { return IsFtlOperational ? FullFtlPropulsionPower : FullStlPropulsionPower; } }

        /// <summary>
        /// The maximum power that can be projected by the STL engines. FullStlSpeed = FullStlPropulsionPower / (Mass * _rigidbody.drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in realtime to a Unity seconds value 
        /// in the EngineRoom using GameTime.GameSpeedAdjustedHoursPerSecond.
        /// </summary>
        public float FullStlPropulsionPower { get { return _enginesStat.FullStlPropulsionPower; } }

        /// <summary>
        /// The maximum power that can be projected by the FTL engines. FullFtlSpeed = FullFtlPropulsionPower / (Mass * _rigidbody.drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in realtime to a Unity seconds value 
        /// in the EngineRoom using GameTime.GameSpeedAdjustedHoursPerSecond.
        /// </summary>
        public float FullFtlPropulsionPower { get { return _enginesStat.FullFtlPropulsionPower; } }

        private Vector3 _requestedHeading;
        /// <summary>
        /// The ship's normalized, requested heading in worldspace coordinates.
        /// </summary>
        public Vector3 RequestedHeading {
            get { return _requestedHeading; }
            set {
                value.ValidateNormalized();
                SetProperty<Vector3>(ref _requestedHeading, value, "RequestedHeading");
            }
        }

        /// <summary>
        /// Readonly. The real-time, normalized heading of the ship in worldspace coordinates. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading { get { return _itemTransform.forward; } }

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

        public override Index3D SectorIndex { get { return References.SectorGrid.GetSectorIndex(Position); } }

        private new ShipHullEquipment HullEquipment { get { return base.HullEquipment as ShipHullEquipment; } }

        private EnginesStat _enginesStat;

        /// <summary>
        /// The speed of the ship in units per hour when it was paused.
        /// </summary>
        private float _currentSpeedValueOnPause;
        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;
        private Rigidbody _shipRigidbody;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipData" /> class.
        /// </summary>
        /// <param name="shipTransform">The ship transform.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        /// <param name="shipRigidbody">The ship rigidbody.</param>
        /// <param name="enginesStat">The engines stat.</param>
        /// <param name="combatStance">The combat stance.</param>
        public ShipData(Transform shipTransform, Player owner, CameraFollowableStat cameraStat, IEnumerable<PassiveCountermeasure> passiveCMs,
            ShipHullEquipment hullEquipment, IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors,
            IEnumerable<ShieldGenerator> shieldGenerators, Rigidbody shipRigidbody, EnginesStat enginesStat, ShipCombatStance combatStance)
            : base(shipTransform, owner, cameraStat, passiveCMs, hullEquipment, activeCMs, sensors, shieldGenerators) {
            _shipRigidbody = shipRigidbody;
            _shipRigidbody.mass = Mass;
            // _shipRigidbody.drag gets set when Topography gets set/changed
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;

            _enginesStat = enginesStat;
            CombatStance = combatStance;
            InitializeLocalValuesAndReferences();
            Subscribe();
        }

        private void InitializeLocalValuesAndReferences() {
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            _requestedHeading = CurrentHeading;  // initialize to something other than Vector3.zero which causes problems with LookRotation
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanging<IGameManager, bool>(gs => gs.IsPaused, IsPausedPropChangingHandler));
            _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, GameSpeedPropChangedHandler));
        }

        public override void CommenceOperations() {
            base.CommenceOperations();
            Topography = References.SectorGrid.GetSpaceTopography(Position);    // will trigger Data.AssessFullSpeedValue()
            //D.Log("{0}.CommenceOperations() setting Topography to {1}.", FullName, Topography.GetValueName());
            IsFtlActivated = true;  // will trigger Data.AssessIsFtlOperational()
        }

        #region Event and Property Change Handlers

        private void IsFtlOperationalPropChangedHandler() {
            string msg = IsFtlOperational ? "now" : "no longer";
            D.Log("{0} FTL is {1} operational.", FullName, msg);
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

        protected override void TopographyPropChangedHandler() {
            base.TopographyPropChangedHandler();
            _shipRigidbody.drag = Drag * Topography.GetRelativeDensity();
            RefreshFullSpeedValue();
        }

        private void IsPausedPropChangingHandler(bool isPausing) {
            if (isPausing) {
                _currentSpeedValueOnPause = CurrentSpeedValue;
            }
        }

        private void GameSpeedPropChangedHandler() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
        }

        #endregion

        /// <summary>
        /// Refreshes the full speed value the ship is capable of achieving.
        /// </summary>
        private void RefreshFullSpeedValue() {
            FullSpeedValue = IsFtlOperational ? FullFtlPropulsionPower / (Mass * _shipRigidbody.drag) : FullStlPropulsionPower / (Mass * _shipRigidbody.drag);
        }

        private void AssessIsFtlOperational() {
            IsFtlOperational = IsFtlActivated && !IsFtlDamaged && !IsFtlDampedByField;
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            _subscriptions.ForAll<IDisposable>(s => s.Dispose());
            _subscriptions.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

