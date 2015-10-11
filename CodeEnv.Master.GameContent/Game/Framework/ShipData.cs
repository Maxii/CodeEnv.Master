// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipData.cs
// Class for Data associated with a ShipItem.
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
    /// Class for Data associated with a ShipItem.
    /// </summary>
    public class ShipData : AUnitElementItemData {

        #region FTL

        // Assume for now that all ships are FTL capable. In the future, I will want some defensive ships to be limited to System space 
        // IMPROVE will need to replace this with FtlShipData-derived class as non-Ftl ships won't be part of fleets, aka FormationStation, etc
        // public bool IsShipFtlCapable { get { return true; } }

        private bool _isFtlOperational;
        /// <summary>
        /// Indicates whether the FTL engines are operational, aka undamaged.
        /// </summary>
        public bool IsFtlOperational {
            get { return _isFtlOperational; }
            set { SetProperty<bool>(ref _isFtlOperational, value, "IsFtlOperational", OnIsFtlOperationalChanged); }
        }

        public bool _isFtlDampedByField;
        /// <summary>
        /// Indicates whether the FTL engines are damped by an FTL Damping Field. 
        /// </summary>
        public bool IsFtlDampedByField {
            get { return _isFtlDampedByField; }
            set { SetProperty<bool>(ref _isFtlDampedByField, value, "IsFtlDampedByField", OnIsFtlDampedByFieldChanged); }
        }

        private bool _isFtlAvailableForUse;
        /// <summary>
        /// Indicates whether the FTL engines are operational and available for use - ie. undamaged, 
        /// not damped by a dampingField and currently located in OpenSpace.
        /// </summary>
        public bool IsFtlAvailableForUse {
            get { return _isFtlAvailableForUse; }
            private set { SetProperty<bool>(ref _isFtlAvailableForUse, value, "IsFtlAvailableForUse"); }
        }

        #endregion

        private bool _isFlapsDeployed;
        public bool IsFlapsDeployed {
            get { return _isFlapsDeployed; }
            set { SetProperty<bool>(ref _isFlapsDeployed, value, "IsFlapsDeployed", OnIsFlapsDeployedChanged); }
        }

        private INavigableTarget _target;
        public INavigableTarget Target {
            get { return _target; }
            set {
                if (_target == value) { return; }   // eliminates equality warning when targets are the same
                SetProperty<INavigableTarget>(ref _target, value, "Target");
            }
        }

        public ShipHullCategory HullCategory { get { return HullEquipment.HullCategory; } }

        // FormationStation moved to ShipItem as it had no apparent value residing in data

        private ShipCombatStance _combatStance; // TODO not currently used
        public ShipCombatStance CombatStance {
            get { return _combatStance; }
            set { SetProperty<ShipCombatStance>(ref _combatStance, value, "CombatStance"); }
        }

        /// <summary>
        /// Readonly. Gets the current speed of the ship in Units per hour. Whether paused or at a GameSpeed
        /// other than Normal (x1), this property always returns the value assuming not paused with GameSpeed.Normal.
        /// </summary>
        public float CurrentSpeed {
            get {
                if (_gameMgr.IsPaused) {
                    return _currentSpeedOnPause;
                }
                else {
                    var speedInGameSpeedAdjustedUnitsPerSec = _rigidbody.velocity.magnitude;
                    var speedInUnitsPerHour = speedInGameSpeedAdjustedUnitsPerSec / _gameTime.GameSpeedAdjustedHoursPerSecond;
                    return speedInUnitsPerHour;
                }
            }
        }

        private float _requestedSpeed;
        /// <summary>
        /// The desired speed this ship should be traveling at in Units per hour. 
        /// The thrust of the ship will be adjusted to accelerate or decelerate to this speed.
        /// </summary>
        public float RequestedSpeed {
            get { return _requestedSpeed; }
            set {
                D.Assert(value <= FullFtlSpeed, "{0} RequestedSpeed {1} > FullFtlSpeed {2}.".Inject(FullName, RequestedSpeed, FullFtlSpeed));
                SetProperty<float>(ref _requestedSpeed, value, "RequestedSpeed");
            }
        }

        private float _drag;
        /// <summary>
        /// The drag on the ship.
        /// </summary>
        public float Drag {
            get { return _drag; }
            set { SetProperty<float>(ref _drag, value, "Drag", OnDragChanged, OnDragChanging); }
        }

        public float FullThrust { get { return IsFtlAvailableForUse ? FullFtlThrust : FullStlThrust; } }

        private float _fullStlThrust;
        /// <summary>
        /// The maximum force projected by the STL engines. FullStlSpeed = FullStlThrust / (Mass * Drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.
        /// </summary>
        public float FullStlThrust {
            get { return _fullStlThrust; }
            set { SetProperty<float>(ref _fullStlThrust, value, "FullStlThrust", OnFullStlThrustChanged); }
        }

        private float _fullFtlThrust;
        /// <summary>
        /// The maximum force projected by the FTL engines. FullFtlSpeed = FullFtlThrust / (Mass * Drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.
        /// </summary>
        public float FullFtlThrust {
            get { return _fullFtlThrust; }
            set { SetProperty<float>(ref _fullFtlThrust, value, "FullFtlThrust", OnFullFtlThrustChanged); }
        }

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
        /// Readonly. The maximum speed that the ship can currently achieve in units per hour.
        /// </summary>
        public float FullSpeed { get { return IsFtlAvailableForUse ? FullFtlSpeed : FullStlSpeed; } }

        private float _fullFtlSpeed;
        /// <summary>
        /// Readonly. Gets the maximum FTL speed that the ship can achieve in units per hour.
        /// Derived directly from FullFtlThrust, mass and drag of the ship.
        /// </summary>
        public float FullFtlSpeed {
            get { return _fullFtlSpeed; }
            private set { SetProperty<float>(ref _fullFtlSpeed, value, "FullFtlSpeed"); }
        }

        private float _fullStlSpeed;
        /// <summary>
        /// Readonly. Gets the maximum STL speed that the ship can achieve in units per hour.
        /// Derived directly from FullStlThrust, mass and drag of the ship.
        /// </summary>
        public float FullStlSpeed {
            get { return _fullStlSpeed; }
            private set { SetProperty<float>(ref _fullStlSpeed, value, "FullStlSpeed"); }
        }

        private float _maxTurnRate;
        /// <summary>
        /// The maximum turn rate of the ship in degrees per hour.
        /// </summary>
        public float MaxTurnRate {
            get { return _maxTurnRate; }
            set { SetProperty<float>(ref _maxTurnRate, value, "MaxTurnRate"); }
        }

        public override Index3D SectorIndex { get { return References.SectorGrid.GetSectorIndex(Position); } }

        protected new ShipHullEquipment HullEquipment { get { return base.HullEquipment as ShipHullEquipment; } }

        /// <summary>
        /// The speed of the ship in units per hour when it was paused.
        /// </summary>
        private float _currentSpeedOnPause;
        private Rigidbody _rigidbody;
        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipData" /> class.
        /// </summary>
        /// <param name="shipTransform">The ship transform.</param>
        /// <param name="hullEquipment">The hull equipment.</param>
        /// <param name="engineStat">The engine stat.</param>
        /// <param name="combatStance">The combat stance.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="activeCMs">The active countermeasures.</param>
        /// <param name="sensors">The sensors.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="shieldGenerators">The shield generators.</param>
        public ShipData(Transform shipTransform, ShipHullEquipment hullEquipment, EngineStat engineStat, ShipCombatStance combatStance, Player owner,
    IEnumerable<ActiveCountermeasure> activeCMs, IEnumerable<Sensor> sensors, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<ShieldGenerator> shieldGenerators)
            : base(shipTransform, hullEquipment, owner, activeCMs, sensors, passiveCMs, shieldGenerators) {

            _rigidbody = shipTransform.rigidbody;
            // rigidbody mass assignment handled by AElementData

            Drag = hullEquipment.Drag;
            Science = hullEquipment.Science;
            Culture = hullEquipment.Culture;
            Income = hullEquipment.Income;

            FullStlThrust = engineStat.FullStlThrust;
            FullFtlThrust = engineStat.FullFtlThrust;
            MaxTurnRate = engineStat.MaxTurnRate;

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
            _subscriptions.Add(_gameMgr.SubscribeToPropertyChanging<IGameManager, bool>(gs => gs.IsPaused, OnIsPausedChanging));
            _subscriptions.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        public override void CommenceOperations() {
            base.CommenceOperations();
            Topography = References.SectorGrid.GetSpaceTopography(Position);
            //D.Log("{0}.CommenceOperations() setting Topography to {1}.", FullName, Topography.GetValueName());
            IsFtlOperational = true;    // will trigger Data.AssessFtlAvailability()
        }

        private void OnIsFtlOperationalChanged() {
            string msg = IsFtlOperational ? "now" : "no longer";
            D.Log("{0} FTL is {1} operational.", FullName, msg);
            AssessFtlAvailability();
        }

        private void OnIsFtlDampedByFieldChanged() {
            AssessFtlAvailability();
        }

        protected override void OnTopographyChanged() {
            base.OnTopographyChanged();
            AssessFtlAvailability();
        }

        private void OnDragChanging(float newDrag) {
            if (Drag != Constants.ZeroF && Drag != _rigidbody.drag) {
                D.Warn("{0}.Drag of {1} and Rigidbody.drag of {2} are not the same.", Name, Drag, _rigidbody.drag);
                // TODO: Need to rethink this whole Drag subject (flaps, FTL, etc.) as I'm probably changing Drag when the flaps are on
            }
        }

        private void OnDragChanged() {
            _rigidbody.drag = IsFlapsDeployed ? Drag / TempGameValues.FlapsMultiplier : Drag;
            OnFullStlThrustChanged();
            OnFullFtlThrustChanged();
        }

        private void OnIsFlapsDeployedChanged() {
            string msg = IsFlapsDeployed ? "deployed" : "retracted";
            D.Log("{0} has {1} flaps.", FullName, msg);
        }

        private void OnFullStlThrustChanged() {
            FullStlSpeed = FullStlThrust / (Mass * Drag);
            D.Log("{0} FullStlSpeed set to {1} units/hour, FullStlThrust = {2}, Mass = {3}, Drag = {4}.", Name, FullStlSpeed, FullStlThrust, Mass, Drag);
        }

        private void OnFullFtlThrustChanged() {
            FullFtlSpeed = FullFtlThrust / (Mass * Drag);
            D.Log("{0} FullFtlSpeed set to {1} units/hour, FullFtlThrust = {2}, Mass = {3}, Drag = {4}.", Name, FullFtlSpeed, FullFtlThrust, Mass, Drag);
        }

        private void OnIsPausedChanging(bool isPausing) {
            if (isPausing) {
                _currentSpeedOnPause = CurrentSpeed;
            }
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
        }

        private void AssessFtlAvailability() {
            IsFtlAvailableForUse = Topography == Topography.OpenSpace && IsFtlOperational && !IsFtlDampedByField;
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

