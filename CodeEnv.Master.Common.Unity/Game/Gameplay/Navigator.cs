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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;


    /// <summary>
    /// Class that provides navigation capabilities to a ship.
    /// </summary>
    public class Navigator : IDisposable {

        private IList<IDisposable> _subscribers;

        private Transform _transform;
        private Rigidbody _rigidbody;
        private GameEventManager _eventMgr;
        private GameTime _gameTime;
        private float _gameSpeedMultiplier;
        private ShipData _data;

        private ThrustHelper _thrustHelper;
        private Vector3 _velocityOnPause;
        private bool _isCurrentHeadingRequestedHeading;
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
            _gameTime = GameTime.Instance;
            Subscribe();
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _thrustHelper = new ThrustHelper(0F, 0F, _data.MaxThrust);
        }

        private void Subscribe() {
            if (_subscribers == null) {
                _subscribers = new List<IDisposable>();
            }
            _subscribers.Add(GameManager.Instance.SubscribeToPropertyChanged<GameManager, bool>(gm => gm.IsGamePaused, OnGamePauseChanged));
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        private void OnGameSpeedChanged() {
            AdjustForGameSpeed(_gameTime.GameSpeed);
        }

        private void OnGamePauseChanged() {
            if (GameManager.Instance.IsGamePaused) {
                _velocityOnPause = _rigidbody.velocity;
                // no angularVelocity needed as it is always zero?
                _rigidbody.isKinematic = true;
                return;
            }
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _velocityOnPause;
            _rigidbody.WakeUp();
        }

        /// <summary>
        /// Adjusts the velocity and thrust of the fleet to reflect the new GameClockSpeed setting. 
        /// The reported speed and directional heading of the fleet is not affected.
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        private void AdjustForGameSpeed(GameClockSpeed gameSpeed) {
            float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _gameSpeedMultiplier = gameSpeed.SpeedMultiplier();
            float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
            // must immediately adjust velocity when game speed changes as just adjusting thrust takes
            // a long time to get to increased/decreased velocity
            if (GameManager.Instance.IsGamePaused) {
                _velocityOnPause = _velocityOnPause * gameSpeedChangeRatio;
            }
            else {
                _rigidbody.velocity = _rigidbody.velocity * gameSpeedChangeRatio;
                // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
            }
        }

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        public void ChangeHeading(Vector3 newHeading) {
            newHeading.ValidateNormalized();
            if (newHeading != _data.RequestedHeading) {
                if (newHeading != _data.CurrentHeading) {
                    _isCurrentHeadingRequestedHeading = false;
                }
                _data.RequestedHeading = newHeading;
                D.Log("Coming to new heading {0}.", newHeading);
                return;
            }
            D.Warn("Duplicate ChangeHeading Command to {0} on ship: {1}.", newHeading, _transform.name);
        }

        /// <summary>
        /// Processes a heading change if needed. Returns true if a turn is underway.
        /// </summary>
        /// <param name="updateRate">The number of frames rendered since this method was last called.</param>
        /// <returns><c>true</c> if a change is underway, false if not</returns>
        public bool TryProcessHeadingChange(int updateRate) {
            if (!_isCurrentHeadingRequestedHeading) {
                float allowedTurn = _data.MaxTurnRate * GameTime.DeltaTimeOrPausedWithGameSpeed * updateRate;
                Vector3 newHeading = Vector3.RotateTowards(_data.CurrentHeading, _data.RequestedHeading, allowedTurn, Constants.ZeroF);
                //Debug.DrawRay(_transform.position, stepDirection, Color.red);
                _transform.rotation = Quaternion.LookRotation(newHeading);
                if (newHeading == _data.RequestedHeading) {
                    D.Log("Turn complete. Now at heading {0}.", newHeading);
                    _isCurrentHeadingRequestedHeading = true;
                }
            }
            return !_isCurrentHeadingRequestedHeading;
        }

        public void ChangeSpeed(float newSpeedRequest) {
            float newSpeed = newSpeedRequest > _data.MaxSpeed ? _data.MaxSpeed : newSpeedRequest;
            float newSpeedToRequestedSpeedRatio = (_data.RequestedSpeed != Constants.ZeroF) ? newSpeed / _data.RequestedSpeed : Constants.ZeroF;
            if (!ThrustHelper.SpeedOnTarget.IsInRange(newSpeedToRequestedSpeedRatio)) {
                _data.RequestedSpeed = newSpeed;
                D.Log("Adjusting thrust to requested speed {0}.", newSpeed);
                float thrustNeededToMaintainRequestedSpeed = newSpeed * _data.Mass * _data.Drag;
                _thrustHelper = new ThrustHelper(newSpeed, thrustNeededToMaintainRequestedSpeed, _data.MaxThrust);
                _thrust = AdjustThrust();
                return;
            }
            D.Warn("Duplicate or over max ChangeSpeed Command to {0} on ship: {1}. MaxSpeed = {2}.", newSpeedRequest, _transform.name, _data.MaxSpeed);
        }

        /// <summary>
        /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        /// call this method from FixedUpdate().
        /// </summary>
        public void ApplyThrust() {
            Vector3 gameSpeedAdjustedThrust = _localTravelDirection * _thrust * _gameSpeedMultiplier;
            _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust);
            _thrust = AdjustThrust();
        }

        private float AdjustThrust() {
            float requestedSpeed = _data.RequestedSpeed;
            if (requestedSpeed == Constants.ZeroF) {
                return Constants.ZeroF;
            }
            float currentSpeed = _data.CurrentSpeed;
            float thrust = _thrustHelper.GetThrust(currentSpeed);
            //D.Log("Current Speed is {0}, > Desired Speed of {1}. New adjusted thrust is {2}.", currentSpeed, requestedSpeed, thrust);
            return thrust;
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

        public class ThrustHelper {

            public static Range<float> SpeedOnTarget = new Range<float>(0.99F, 1.01F);

            private static Range<float> _speedWayAboveTarget = new Range<float>(1.10F, 10.0F);
            //private static Range<float> _speedModeratelyAboveTarget = new Range<float>(1.10F, 1.25F);
            private static Range<float> _speedSlightlyAboveTarget = new Range<float>(1.01F, 1.10F);
            //private static Range<float> _speedSlightlyBelowTarget = new Range<float>(0.90F, 0.99F);
            //private static Range<float> _speedModeratelyBelowTarget = new Range<float>(0.75F, 0.90F);
            private static Range<float> _speedWayBelowTarget = new Range<float>(0.0F, 0.99F);

            private float _requestedSpeed;

            //private float _targetThrustMinusMinus;
            private float _targetThrustMinus;
            private float _targetThrust;
            //private float _targetThrustPlus;
            //private float _targetThrustPlusPlus;
            private float _maxThrust;

            public ThrustHelper(float requestedSpeed, float targetThrust, float maxThrust) {
                _requestedSpeed = requestedSpeed;

                //_targetThrustMinusMinus = Mathf.Min(targetThrust / _speedModeratelyAboveTarget.Max, maxThrust);
                _targetThrustMinus = Mathf.Min(targetThrust / _speedSlightlyAboveTarget.Max, maxThrust);
                _targetThrust = Mathf.Min(targetThrust, maxThrust);
                //_targetThrustPlus = Mathf.Min(targetThrust / _speedSlightlyBelowTarget.Min, maxThrust);
                // _targetThrustPlusPlus = Mathf.Min(targetThrust / _speedModeratelyBelowTarget.Min, maxThrust);
                _maxThrust = maxThrust;
            }

            public float GetThrust(float currentSpeed) {
                if (_requestedSpeed == Constants.ZeroF) { return Constants.ZeroF; }

                float sr = currentSpeed / _requestedSpeed;
                if (SpeedOnTarget.IsInRange(sr)) { return _targetThrust; }
                //if (_speedSlightlyBelowTarget.IsInRange(sr)) { return _targetThrustPlus; }
                if (_speedSlightlyAboveTarget.IsInRange(sr)) { return _targetThrustMinus; }
                //if (_speedModeratelyBelowTarget.IsInRange(sr)) { return _targetThrustPlusPlus; }
                //if (_speedModeratelyAboveTarget.IsInRange(sr)) { return _targetThrustMinusMinus; }
                if (_speedWayBelowTarget.IsInRange(sr)) { return _maxThrust; }
                if (_speedWayAboveTarget.IsInRange(sr)) { return Constants.ZeroF; }
                return Constants.ZeroF;
            }
        }
    }
}

