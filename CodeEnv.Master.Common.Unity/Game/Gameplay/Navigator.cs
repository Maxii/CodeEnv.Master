// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Navigator.cs
// Class that provides navigation capabilities to a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Class that provides navigation capabilities to a ship.
    /// </summary>
    public class Navigator : IDisposable {

        /// <summary>
        /// Readonly. Gets the current speed of the ship in Units per
        /// day, normalized for game speed.
        /// </summary>
        public float CurrentSpeed {
            get { return _rigidbody.velocity.magnitude / _gameSpeed; }
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
                _requestedSpeed = value > _data.MaxSpeed ? _data.MaxSpeed : value;
                _thrust = _requestedSpeed > CurrentSpeed ? _data.MaxThrust : _thrust;
                // ApplyThrust() handles deceleration
            }
        }

        private Transform _transform;
        private Rigidbody _rigidbody;
        private GameEventManager _eventMgr;
        private float _gameSpeed;
        private ShipData _data;

        private bool _isHeadingDirty;
        private Vector3 _requestedHeading;
        // ship always travels forward - the direction it is pointed. Thrust direction is opposite
        private Vector3 _localTravelDirection = new Vector3(0F, 0F, 1F);
        private float _thrust;


        /// <summary>
        /// Initializes a new instance of the <see cref="Navigator"/> class.
        /// </summary>
        /// <param name="t">Ship Transform</param>
        /// <param name="data">Ship data.</param>
        public Navigator(Transform t, ShipData data) {
            _transform = t;
            _data = data;
            _rigidbody = t.rigidbody;
            _rigidbody.useGravity = false;
            _eventMgr = GameEventManager.Instance;
            AddListeners();
            _gameSpeed = GameTime.GameSpeed.SpeedMultiplier();

            _requestedHeading = _transform.forward;
        }

        private void AddListeners() {
            _eventMgr.AddListener<GameSpeedChangeEvent>(this, OnGameSpeedChange);
        }

        private void OnGameSpeedChange(GameSpeedChangeEvent e) {
            AdjustForGameSpeed(e.GameSpeed);
        }

        /// <summary>
        /// Adjusts the velocity and thrust of the fleet to reflect the new GameClockSpeed setting. 
        /// The reported speed and directional heading of the fleet is not affected.
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        private void AdjustForGameSpeed(GameClockSpeed gameSpeed) {
            float previousGameSpeed = _gameSpeed;
            _gameSpeed = gameSpeed.SpeedMultiplier();
            Debug.Log("GameSpeedMultiplier changed to {0}.".Inject(_gameSpeed));
            float gameSpeedChangeRatio = _gameSpeed / previousGameSpeed;
            // must immediately adjust velocity when game speed changes as just adjusting thrust takes
            // a long time to get to increased/decreased velocity
            _rigidbody.velocity = _rigidbody.velocity * gameSpeedChangeRatio;
            // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
        }

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        public void ChangeHeading(Vector3 newHeading) {
            newHeading.ValidateNormalized();
            if (newHeading != _requestedHeading) {
                _isHeadingDirty = true;
                _requestedHeading = newHeading;
            }
            D.Log("Changing Heading to {0}.", newHeading);
        }

        /// <summary>
        /// Processes a heading change if one is in process.
        /// </summary>
        /// <param name="updateRate">The number of frames rendered since this method was last called.</param>
        /// <returns><c>true</c> if a change was processed, false if not</returns>
        public bool TryProcessHeadingChange(int updateRate) {
            if (_isHeadingDirty) {
                float allowedTurn = _data.MaxTurnRate * GameTime.DeltaTimeOrPaused * updateRate * _gameSpeed;
                Vector3 newHeading = Vector3.RotateTowards(_transform.forward, _requestedHeading, allowedTurn, Constants.ZeroF);
                //Debug.DrawRay(_transform.position, stepDirection, Color.red);
                _transform.rotation = Quaternion.LookRotation(newHeading);
                if (newHeading == _requestedHeading) {
                    _isHeadingDirty = false;
                    return true;
                }
            }
            return _isHeadingDirty;
        }

        /// <summary>
        /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        /// call this method from FixedUpdate().
        /// </summary>
        public void ApplyThrust() {
            Vector3 gameSpeedAdjustedThrust = _localTravelDirection * _thrust * _gameSpeed;
            _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust);
            //D.Log("AdjustedThrust is {0}. Actual velocity is {1}.".Inject(gameSpeedAdjustedThrust, _rigidbody.velocity));
            if (CurrentSpeed > RequestedSpeed) {
                _thrust = RequestedSpeed * _rigidbody.mass * _rigidbody.drag;
                D.Log("Current Speed is {0}, > Desired Speed of {1}. New nonadjusted thrust is {2}.", CurrentSpeed, RequestedSpeed, _thrust);
            }
        }

        private void RemoveListeners() {
            _eventMgr.RemoveListener<GameSpeedChangeEvent>(this, OnGameSpeedChange);
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
                RemoveListeners();
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

