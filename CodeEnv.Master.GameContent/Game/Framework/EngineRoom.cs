// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EngineRoom.cs
// Runs the engines of a ship generating thrust.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Runs the engines of a ship generating thrust.
    /// </summary>
    public class EngineRoom : IDisposable {

        public static Range<float> SpeedTargetRange = new Range<float>(0.99F, 1.01F);

        private static Range<float> _speedWayAboveTarget = new Range<float>(1.10F, 10.0F);
        //private static Range<float> _speedModeratelyAboveTarget = new Range<float>(1.10F, 1.25F);
        private static Range<float> _speedSlightlyAboveTarget = new Range<float>(1.01F, 1.10F);
        private static Range<float> _speedSlightlyBelowTarget = new Range<float>(0.90F, 0.99F);
        //private static Range<float> _speedModeratelyBelowTarget = new Range<float>(0.75F, 0.90F);
        private static Range<float> _speedWayBelowTarget = new Range<float>(0.0F, 0.90F);

        //private float _targetThrustMinusMinus;
        private float _targetThrustMinus;
        private float _targetThrust;
        private float _targetThrustPlus;

        private bool _isFlapsDeployed;

        private Vector3 _localTravelDirection = new Vector3(0F, 0F, 1F);
        private float _gameSpeedMultiplier;
        private Vector3 _velocityOnPause;

        private ShipData _data;
        private Rigidbody _rigidbody;

        private Job _job;
        private IList<IDisposable> _subscribers;

        public EngineRoom(ShipData data, Rigidbody rigidbody) {
            _data = data;
            _rigidbody = rigidbody;
            _rigidbody.useGravity = false;
            _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            Subscribe();
        }

        private void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
            _subscribers.Add(GameStatus.Instance.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsPaused, OnIsPausedChanged));
        }

        public bool ChangeSpeed(float newSpeedRequest) {
            if (_isFlapsDeployed) {
                // reset drag to normal so max speed and thrust calculations are accurate
                // they will be applied again during GetThrust() if needed
                DeployFlaps(false);
            }
            newSpeedRequest = Mathf.Clamp(newSpeedRequest, Constants.ZeroF, _data.MaxSpeed);
            float previousRequestedSpeed = _data.RequestedSpeed;
            float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? newSpeedRequest / previousRequestedSpeed : Constants.ZeroF;
            if (EngineRoom.SpeedTargetRange.Contains(newSpeedToRequestedSpeedRatio)) {
                D.Warn("{0} is already generating thrust for {1}. Requested speed unchanged.", _data.Name, newSpeedRequest);
                return false;
            }
            SetThrustFor(newSpeedRequest);
            D.Log("{0} adjusting thrust to achieve requested speed {1}.", _data.Name, newSpeedRequest);

            if (_job == null || !_job.IsRunning) {
                _job = new Job(OperateEngines(), toStart: true, onJobComplete: delegate {
                    string message = "{0} thrust stopped.  Coasting speed is {1}.";
                    D.Log(message, _data.Name, _data.CurrentSpeed);
                });
            }
            return true;
        }

        private void OnGameSpeedChanged() {
            float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _gameSpeedMultiplier = GameTime.Instance.GameSpeed.SpeedMultiplier();
            float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
            AdjustForGameSpeed(gameSpeedChangeRatio);
        }

        private void OnIsPausedChanged() {
            if (GameStatus.Instance.IsPaused) {
                _velocityOnPause = _rigidbody.velocity;
                _rigidbody.isKinematic = true;
            }
            else {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = _velocityOnPause;
                _rigidbody.WakeUp();
            }
        }

        private void SetThrustFor(float requestedSpeed) {
            _data.RequestedSpeed = requestedSpeed;
            float targetThrust = requestedSpeed * _data.Drag * _data.Mass;

            //_targetThrustMinusMinus = Mathf.Min(targetThrust / _speedModeratelyAboveTarget.Max, maxThrust);
            _targetThrustMinus = Mathf.Min(targetThrust / _speedSlightlyAboveTarget.Max, _data.MaxThrust);
            _targetThrust = Mathf.Min(targetThrust, _data.MaxThrust);
            _targetThrustPlus = Mathf.Min(targetThrust / _speedSlightlyBelowTarget.Min, _data.MaxThrust);
            // _targetThrustPlusPlus = Mathf.Min(targetThrust / _speedModeratelyBelowTarget.Min, maxThrust);
        }

        private float GetThrust() {
            if (_data.RequestedSpeed == Constants.ZeroF) {
                DeployFlaps(true);
                return Constants.ZeroF;
            }

            float sr = _data.CurrentSpeed / _data.RequestedSpeed;
            if (SpeedTargetRange.Contains(sr)) {
                DeployFlaps(false);
                return _targetThrust;
            }
            if (_speedSlightlyBelowTarget.Contains(sr)) {
                DeployFlaps(false);
                return _targetThrustPlus;
            }
            if (_speedSlightlyAboveTarget.Contains(sr)) {
                DeployFlaps(false);
                return _targetThrustMinus;
            }
            //if (_speedModeratelyBelowTarget.IsInRange(sr)) { return _targetThrustPlusPlus; }
            //if (_speedModeratelyAboveTarget.IsInRange(sr)) { return _targetThrustMinusMinus; }
            if (_speedWayBelowTarget.Contains(sr)) {
                DeployFlaps(false);
                return _data.MaxThrust;
            }
            if (_speedWayAboveTarget.Contains(sr)) {
                DeployFlaps(true);
                return Constants.ZeroF;
            }
            return Constants.ZeroF;
        }

        private void DeployFlaps(bool toDeploy) {
            if (!_isFlapsDeployed && toDeploy) {
                _data.Drag = _data.Drag * 10F;
                _isFlapsDeployed = true;
                D.Log("{0} has deployed flaps.", _data.Name);
            }
            else if (_isFlapsDeployed && !toDeploy) {
                _data.Drag = _data.Drag * 0.1F;
                _isFlapsDeployed = false;
                D.Log("{0} has retracted flaps.", _data.Name);
            }
        }

        /// <summary>
        /// Coroutine that continuously applies thrust while RequestedSpeed is not Zero.
        /// </summary>
        /// <returns></returns>
        private IEnumerator OperateEngines() {
            while (_data.RequestedSpeed != Constants.ZeroF) {
                ApplyThrust();
                yield return new WaitForFixedUpdate();
            }
            DeployFlaps(true);
        }

        /// <summary>
        /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        /// call this method at a pace consistent with FixedUpdate().
        /// </summary>
        private void ApplyThrust() {
            Vector3 gameSpeedAdjustedThrust = _localTravelDirection * GetThrust() * _gameSpeedMultiplier;
            _rigidbody.AddRelativeForce(gameSpeedAdjustedThrust);
            //D.Log("Speed is now {0}.", _data.CurrentSpeed);
        }

        /// <summary>
        /// Adjusts the velocity and thrust of the ship to reflect the new GameClockSpeed setting. 
        /// The reported speed and directional heading of the ship is not affected.
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        private void AdjustForGameSpeed(float gameSpeedChangeRatio) {
            // must immediately adjust velocity when game speed changes as just adjusting thrust takes
            // a long time to get to increased/decreased velocity
            if (GameStatus.Instance.IsPaused) {
                _velocityOnPause = _velocityOnPause * gameSpeedChangeRatio;
            }
            else {
                _rigidbody.velocity = _rigidbody.velocity * gameSpeedChangeRatio;
                // drag should not be adjusted as it will change the velocity that can be supported by the adjusted thrust
            }
        }

        private void Cleanup() {
            Unsubscribe();
            if (_job != null) {
                _job.Kill();
            }
            // other cleanup here including any tracking Gui2D elements
        }

        private void Unsubscribe() {
            _subscribers.ForAll(d => d.Dispose());
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

