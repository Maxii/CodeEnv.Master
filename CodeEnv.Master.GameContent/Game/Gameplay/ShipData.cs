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
    public class ShipData : AElementData<ShipCategory>, IDisposable {
        //public class ShipData : AMortalData, IDisposable {

        //public ShipCategory Category { get; private set; }

        /// <summary>
        /// The local position of this ship within the fleet formation.
        /// </summary>
        //public Vector3 FormationPosition { get; set; }

        /// <summary>
        /// Readonly. Gets the current speed of the ship in Units per
        /// day, normalized for game speed.
        /// </summary>
        public float CurrentSpeed {
            get { return (_gameStatus.IsPaused) ? _currentSpeedOnPause : _rigidbody.velocity.magnitude / _gameSpeedMultiplier; }
        }

        private float _requestedSpeed;
        /// <summary>
        /// Gets or sets the desired speed this ship should
        /// be traveling at in Units per day. The thrust of the ship will be adjusted
        /// to accelerate or decelerate to this speed.
        /// </summary>
        public float RequestedSpeed {
            get { return _requestedSpeed; }
            set {
                value = value < MaxSpeed ? value : MaxSpeed;
                SetProperty<float>(ref _requestedSpeed, value, "RequestedSpeed");
            }
        }

        private float _drag;
        /// <summary>
        /// The drag on the ship.
        /// </summary>
        public float Drag {
            get { return _drag; }
            set { SetProperty<float>(ref _drag, value, "Drag", OnDragChanged); }
        }

        private float _maxThrust;
        /// <summary>
        /// Gets or sets the max thrust achievable by the engines.
        /// </summary>
        public float MaxThrust {
            get { return _maxThrust; }
            set {
                SetProperty<float>(ref _maxThrust, value, "MaxThrust", OnMaxThrustChanged);
            }
        }

        private Vector3 _requestedHeading;
        /// <summary>
        /// Gets or sets the ship's requested heading, normalized.
        /// </summary>
        public Vector3 RequestedHeading {
            get { return _requestedHeading; }
            set {
                value = value.normalized;
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

        private float _maxSpeed;
        /// <summary>
        /// Readonly. Gets the maximum speed that the ship can achieve in units per day.
        /// Derived directly from the MaxThrust, mass and drag of the ship.
        /// </summary>
        public float MaxSpeed {
            get {
                if (_maxSpeed == Constants.ZeroF) { _maxSpeed = MaxThrust / (Mass * Drag); }
                return _maxSpeed;
            }
        }

        private float _maxTurnRate;
        /// <summary>
        /// Gets or sets the maximum turn rate of the ship in degrees per day.
        /// </summary>
        public float MaxTurnRate {
            get { return _maxTurnRate; }
            set {
                SetProperty<float>(ref _maxTurnRate, value, "MaxTurnRate");
            }
        }

        //private IPlayer _owner;
        //public IPlayer Owner {
        //    get { return _owner; }
        //    set {
        //        SetProperty<IPlayer>(ref _owner, value, "Owner");
        //    }
        //}

        //private CombatStrength _combatStrength;
        //public CombatStrength Strength {
        //    get { return _combatStrength; }
        //    set {
        //        SetProperty<CombatStrength>(ref _combatStrength, value, "Strength");
        //    }
        //}

        //public float Mass { get; private set; }

        private float _currentSpeedOnPause;
        private Rigidbody _rigidbody;
        private IList<IDisposable> _subscribers;
        private GameStatus _gameStatus;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipData" /> class.
        /// </summary>
        /// <param name="category">The category of ship.</param>
        /// <param name="shipName">Name of the ship.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="drag">The drag.</param>
        public ShipData(ShipCategory category, string shipName, float maxHitPoints, float mass, float drag)
            : base(category, shipName, maxHitPoints, mass) {
            //Category = category;
            // Mass = mass;
            _drag = drag;   // avoid OnDragChanged as the rigidbody is not yet known
            Initialize();
        }

        private void Initialize() {
            _gameStatus = GameStatus.Instance;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            Subscribe();
            D.Log("{0} ShipData constructor has completed.", Name);
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameStatus.SubscribeToPropertyChanging<GameStatus, bool>(gs => gs.IsPaused, OnIsPausedChanging));
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        protected override void OnTransformChanged() {
            base.OnTransformChanged();
            _rigidbody = Transform.rigidbody;
            _rigidbody.mass = Mass;
            _rigidbody.drag = Drag;
        }

        private void OnDragChanged() {
            _rigidbody.drag = Drag;
            _maxSpeed = MaxThrust / (Mass * Drag);
        }

        private void OnMaxThrustChanged() {
            _maxSpeed = MaxThrust / (Mass * Drag);
        }

        private void OnIsPausedChanging(bool isPausing) {
            if (isPausing) {
                // game is about to pause
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

