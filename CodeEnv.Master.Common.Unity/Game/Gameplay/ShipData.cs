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

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// All the data associated with a particular ship.
    /// <remarks>MaxSpeed = MaxThrust / (Mass * Drag)</remarks>
    /// </summary>
    public class ShipData : Data, IDisposable {

        /// <summary>
        /// Readonly. Gets the current speed of the ship in Units per
        /// day, normalized for game speed.
        /// </summary>
        public float CurrentSpeed {
            get { return (_gameMgr.IsGamePaused) ? _currentSpeedOnPause : _rigidbody.velocity.magnitude / _gameSpeedMultiplier; }
        }

        private float _requestedSpeed;
        /// <summary>
        /// Gets or sets the desired speed this ship should
        /// be traveling at. The thrust of the ship will be adjusted
        /// to accelerate or decelerate to this speed.
        /// </summary>
        public float RequestedSpeed {
            get { return _requestedSpeed; }
            set {
                value = value < MaxSpeed ? value : MaxSpeed;
                SetProperty<float>(ref _requestedSpeed, value, "RequestedSpeed");
            }
        }

        /// <summary>
        /// Readonly. The mass of the ship.
        /// </summary>
        public float Mass {
            get { return _rigidbody.mass; }
        }

        /// <summary>
        /// Readonly. The drag on the ship.
        /// </summary>
        public float Drag {
            get { return _rigidbody.drag; }
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

        private void OnMaxThrustChanged() {
            _maxSpeed = MaxThrust / (Mass * Drag);
        }

        private Vector3 _requestedHeading;
        /// <summary>
        /// Gets or sets the ship's heading.
        /// </summary>
        public Vector3 RequestedHeading {
            get { return _requestedHeading; }
            set {
                SetProperty<Vector3>(ref _requestedHeading, value, "RequestedHeading");
            }
        }

        /// <summary>
        /// Readonly. The real-time heading of the ship. Equivalent to transform.forward.
        /// </summary>
        public Vector3 CurrentHeading {
            get {
                return _transform.forward;
            }
        }

        private float _maxSpeed;
        /// <summary>
        /// Readonly. Gets the max speed that can be achieved, derived directly
        /// from the MaxThrust, mass and drag of the ship.
        /// </summary>
        public float MaxSpeed {
            get {
                if (_maxSpeed == Constants.ZeroF) { _maxSpeed = MaxThrust / (Mass * Drag); }
                return _maxSpeed;
            }
        }

        private float _maxTurnRate;
        /// <summary>
        /// Gets or sets the max turn rate of the ship.
        /// </summary>
        public float MaxTurnRate {
            get { return _maxTurnRate; }
            set {
                SetProperty<float>(ref _maxTurnRate, value, "MaxTurnRate");
            }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set {
                SetProperty<IPlayer>(ref _owner, value, "Owner");
            }
        }

        private float _health;
        public float Health {
            get { return _health; }
            set {
                SetProperty<float>(ref _health, value, "Health");
            }
        }

        private float _maxHitPoints;
        public float MaxHitPoints {
            get { return _maxHitPoints; }
            set {
                SetProperty<float>(ref _maxHitPoints, value, "MaxHitPoints");
            }
        }

        private CombatStrength _combatStrength;
        public CombatStrength Strength {
            get { return _combatStrength; }
            set {
                SetProperty<CombatStrength>(ref _combatStrength, value, "Strength");
            }
        }

        private ShipHull _hull;
        public ShipHull Hull {
            get { return _hull; }
            set {
                SetProperty<ShipHull>(ref _hull, value, "Hull");
            }
        }

        private float _currentSpeedOnPause;
        private Rigidbody _rigidbody;
        private IList<IDisposable> _subscribers;
        private GameManager _gameMgr;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;

        public ShipData(Transform t)
            : base(t) {
            _rigidbody = t.rigidbody;
            _gameMgr = GameManager.Instance;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            Subscribe();
            D.Log("ShipData constructor {0} has completed.", t.name);
        }

        private void Subscribe() {
            if (_subscribers == null) {
                _subscribers = new List<IDisposable>();
            }
            _subscribers.Add(_gameMgr.SubscribeToPropertyChanging<GameManager, bool>(gm => gm.IsGamePaused, OnPauseStateChanging));
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        private void OnPauseStateChanging() {
            bool isGamePausedPriorToChange = _gameMgr.IsGamePaused;
            if (!isGamePausedPriorToChange) {
                // game is about to pause
                _currentSpeedOnPause = CurrentSpeed;
            }
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
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
                Unsubscribe();
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

