// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipData.cs
// All the data associated with a particular ship.
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
    /// All the data associated with a particular ship.
    /// <remarks>MaxSpeed = MaxThrust / (Mass * Drag)</remarks>
    /// </summary>
    public class ShipData : AElementData, IDisposable {

        #region FTL

        // Assume for now that all ships are FTL capable. In the future, I will want some defensive ships to be limited to System space 
        // IMPROVE will need to replace this with FtlShipData-derived class as non-Ftl ships won't be part of fleets, aka FormationStation, etc
        // public bool IsShipFtlCapable { get { return true; } }

        private bool _isFtlDamaged;
        /// <summary>
        /// Indicates whether the FTL engines are damaged.
        /// </summary>
        public bool IsFtlDamaged {
            get { return _isFtlDamaged; }
            set { SetProperty<bool>(ref _isFtlDamaged, value, "IsFtlDamaged", OnIsFtlDamagedChanged); }
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
        /// Indicates whether the FTL engines are available for use - ie. undamaged, 
        /// not damped by a dampingField and currently located in OpenSpace.
        /// </summary>
        public bool IsFtlAvailableForUse {
            get { return _isFtlAvailableForUse; }
            set { SetProperty<bool>(ref _isFtlAvailableForUse, value, "IsFtlAvailableForUse"); }
        }

        #endregion

        private bool _isFlapsDeployed;
        public bool IsFlapsDeployed {
            get { return _isFlapsDeployed; }
            set { SetProperty<bool>(ref _isFlapsDeployed, value, "IsFlapsDeployed", OnIsFlapsDeployedChanged); }
        }

        private IDestinationTarget _target;
        public IDestinationTarget Target {
            get { return _target; }
            set { SetProperty<IDestinationTarget>(ref _target, value, "Target"); }
        }

        public ShipCategory Category { get; private set; }

        /// <summary>
        /// The station in the formation this ship is currently assigned too.
        /// </summary>
        public IFormationStation FormationStation { get; set; }

        public ShipCombatStance CombatStance { get; set; }  // TODO not currently used

        /// <summary>
        /// Readonly. Gets the current speed of the ship in Units per hour, normalized for game speed.
        /// </summary>
        public float CurrentSpeed {
            get { return (_gameStatus.IsPaused) ? _currentSpeedOnPause : (_rigidbody.velocity.magnitude / GeneralSettings.Instance.HoursPerSecond) / _gameSpeedMultiplier; }
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
        /// Gets or sets the ship's requested heading, normalized.
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
        public Vector3 CurrentHeading {
            get {
                return Transform.forward;
            }
        }

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

        private float _currentSpeedOnPause;
        private Rigidbody _rigidbody;
        private IList<IDisposable> _subscribers;
        private GameStatus _gameStatus;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipData"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public ShipData(ShipStat stat)
            : base(stat.Name, stat.Mass, stat.MaxHitPoints) {
            _drag = stat.Drag;  // avoid OnDragChanged as the rigidbody is not yet known
            Category = stat.Category;
            CombatStance = stat.CombatStance;
            FullStlThrust = stat.FullStlThrust;
            FullFtlThrust = stat.FullFtlThrust;
            MaxTurnRate = stat.MaxTurnRate;
            Initialize();
        }

        private void Initialize() {
            _gameStatus = GameStatus.Instance;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameStatus.SubscribeToPropertyChanging<GameStatus, bool>(gs => gs.IsPaused, OnIsPausedChanging));
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        /// <summary>
        /// Assesses the availability of the ship's FTL Engines. This must always be called immediately after Topography is potentially changed as there is no
        /// OnTopographyChanged() method - see note.
        /// 
        /// Note: Must be public and handled this way as Topography Property will not reliably generate OnTopographyChanged() as default(Topography) 
        /// is OpenSpace rather than None. Can't use None = 0, as OpenSpace = 0 is used to generate a Pathfinding bitmask tag used to assign penalty values.
        /// </summary>
        public void AssessFtlAvailability() {
            IsFtlAvailableForUse = Topography == SpaceTopography.OpenSpace && !IsFtlDamaged && !IsFtlDampedByField;
        }

        protected override void OnTransformChanged() {
            base.OnTransformChanged();
            _rigidbody = Transform.rigidbody;
            _rigidbody.mass = Mass;
            _rigidbody.drag = Drag;
            _requestedHeading = CurrentHeading;  // initialize to something other than Vector3.zero which causes problems with LookRotation
        }

        private void OnIsFtlDamagedChanged() {
            AssessFtlAvailability();
        }

        private void OnIsFtlDampedByFieldChanged() {
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

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _subscribers.ForAll<IDisposable>(s => s.Dispose());
            _subscribers.Clear();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        [DoNotSerialize]
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here

            alreadyDisposed = true;
        }

        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

    }
}

