﻿// --------------------------------------------------------------------------------------------------------------------
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

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
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
        private GameStatus _gameStatus;
        private GeneralSettings _generalSettings;
        private float _gameSpeedMultiplier;
        private ShipData _data;

        private Job _speedJob;
        private Job _headingJob;

        private ThrustHelper _thrustHelper;
        private Vector3 _velocityOnPause;
        // ship always travels forward - the direction it is pointed. Thrust direction is opposite
        private Vector3 _localTravelDirection = new Vector3(0F, 0F, 1F);
        private float _thrust;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipNavigator"/> class.
        /// </summary>
        /// <param name="t">Ship Transform</param>
        /// <param name="data">Ship data.</param>
        public ShipNavigator(Transform t, ShipData data) {
            _transform = t;
            _data = data;
            _rigidbody = t.rigidbody;
            _rigidbody.useGravity = false;
            _eventMgr = GameEventManager.Instance;
            _gameTime = GameTime.Instance;
            _gameStatus = GameStatus.Instance;
            _generalSettings = GeneralSettings.Instance;
            Subscribe();
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _thrustHelper = new ThrustHelper(0F, 0F, _data.FullStlThrust);
        }

        private void Subscribe() {
            if (_subscribers == null) {
                _subscribers = new List<IDisposable>();
            }
            _subscribers.Add(_gameStatus.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsPaused, OnIsPausedChanged));
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        private void OnGameSpeedChanged() {
            AdjustForGameSpeed(_gameTime.GameSpeed);
        }

        private void OnIsPausedChanged() {
            if (_gameStatus.IsPaused) {
                _velocityOnPause = _rigidbody.velocity;
                // no angularVelocity needed as it is always zero?
                _rigidbody.isKinematic = true;
            }
            else {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = _velocityOnPause;
                _rigidbody.WakeUp();
            }
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
            if (_gameStatus.IsPaused) {
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
        /// <returns><c>true</c> if the command was accepted, <c>false</c> if the command is a duplicate.</returns>
        public bool ChangeHeading(Vector3 newHeading) {
            newHeading.ValidateNormalized();
            if (Mathfx.Approx(newHeading, _data.RequestedHeading, 0.01F)) {
                //if (newHeading.IsSameDirection(_data.RequestedHeading)) { // too precise
                D.Warn("Duplicate ChangeHeading Command to {0} on {1}.", newHeading, _data.Name);
                return false;
            }
            _data.RequestedHeading = newHeading;
            if (_headingJob != null && _headingJob.IsRunning) {
                _headingJob.Kill();
            }
            _headingJob = new Job(ExecuteHeadingChange(), toStart: true, onJobComplete: (wasKilled) => {
                string message = "Turn complete. {0} current heading is {1}.";
                if (wasKilled) {
                    message = "{0} turn command cancelled. Current Heading is {1}.";
                }
                D.Log(message, _data.Name, _data.CurrentHeading);
            });
            return true;
        }

        /// <summary>
        /// Coroutine that executes a heading change. 
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExecuteHeadingChange() {
            int previousFrameCount = Time.frameCount;
            float maxTurnRatePerSecond = _data.MaxTurnRate * _generalSettings.DaysPerSecond;
            //D.Log("New coroutine. {0} coming to heading {1} at {2} radians/day.", _data.Name, _data.RequestedHeading, _data.MaxTurnRate);
            while (!IsTurnComplete()) {
                int framesSinceLastPass = Time.frameCount - previousFrameCount;
                previousFrameCount = Time.frameCount;
                float allowedTurn = maxTurnRatePerSecond * GameTime.DeltaTimeOrPausedWithGameSpeed * framesSinceLastPass;
                Vector3 newHeading = Vector3.RotateTowards(_data.CurrentHeading, _data.RequestedHeading, allowedTurn, Constants.ZeroF);
                _transform.rotation = Quaternion.LookRotation(newHeading);
                yield return new WaitForSeconds(0.5F);
            }
        }

        private bool IsTurnComplete() {
            //D.Log("{0} heading passing {1} toward {2}.", _data.Name, _data.CurrentHeading, _data.RequestedHeading);
            // don't worry about the turn passing through this test because it is so precise. The coroutine will home on the requested heading until this is satisfied
            return _data.CurrentHeading.IsSameDirection(_data.RequestedHeading);
        }

        public bool ChangeSpeed(float newSpeedRequest) {
            float newSpeed = Mathf.Clamp(newSpeedRequest, Constants.ZeroF, _data.FullStlSpeed);
            float previousRequestedSpeed = _data.RequestedSpeed;
            float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? newSpeed / previousRequestedSpeed : Constants.ZeroF;
            if (ThrustHelper.SpeedTargetRange.Contains(newSpeedToRequestedSpeedRatio)) {
                D.Warn("{1} ChangeSpeed Command to {0} (Max = {2}) not executed. Target speed unchanged.", newSpeedRequest, _transform.name, _data.FullStlSpeed);
                return false;
            }
            _data.RequestedSpeed = newSpeed;
            float thrustNeededToMaintainRequestedSpeed = newSpeed * _data.Mass * _data.Drag;
            _thrustHelper = new ThrustHelper(newSpeed, thrustNeededToMaintainRequestedSpeed, _data.FullStlThrust);
            D.Log("{0} adjusting thrust to achieve requested speed {1}.", _data.Name, newSpeed);
            _thrust = AdjustThrust();

            if (_speedJob == null || !_speedJob.IsRunning) {
                _speedJob = new Job(ExecuteSpeedChange(), toStart: true, onJobComplete: delegate {
                    string message = "{0} thrust stopped.  Coasting speed is {1}.";
                    D.Log(message, _data.Name, _data.CurrentSpeed);
                });
            }
            return true;
        }

        /// <summary>
        /// Coroutine that continuously applies thrust unless RequestedSpeed is Zero.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExecuteSpeedChange() {
            while (_data.RequestedSpeed != Constants.ZeroF) {
                ApplyThrust();
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Applies Thrust (direction and magnitude), adjusted for game speed. Clients should
        /// call this method at a pace consistent with FixedUpdate().
        /// </summary>
        private void ApplyThrust() {
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

        private void Cleanup() {
            Unsubscribe();
            if (_headingJob != null) {
                _headingJob.Kill();
            }
            if (_speedJob != null) {
                _speedJob.Kill();
            }
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

        public class ThrustHelper {

            public static ValueRange<float> SpeedTargetRange = new ValueRange<float>(0.99F, 1.01F);

            private static ValueRange<float> _speedWayAboveTarget = new ValueRange<float>(1.10F, 10.0F);
            //private static Range<float> _speedModeratelyAboveTarget = new Range<float>(1.10F, 1.25F);
            private static ValueRange<float> _speedSlightlyAboveTarget = new ValueRange<float>(1.01F, 1.10F);
            //private static Range<float> _speedSlightlyBelowTarget = new Range<float>(0.90F, 0.99F);
            //private static Range<float> _speedModeratelyBelowTarget = new Range<float>(0.75F, 0.90F);
            private static ValueRange<float> _speedWayBelowTarget = new ValueRange<float>(0.0F, 0.99F);

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
                if (SpeedTargetRange.Contains(sr)) { return _targetThrust; }
                //if (_speedSlightlyBelowTarget.IsInRange(sr)) { return _targetThrustPlus; }
                if (_speedSlightlyAboveTarget.Contains(sr)) { return _targetThrustMinus; }
                //if (_speedModeratelyBelowTarget.IsInRange(sr)) { return _targetThrustPlusPlus; }
                //if (_speedModeratelyAboveTarget.IsInRange(sr)) { return _targetThrustMinusMinus; }
                if (_speedWayBelowTarget.Contains(sr)) { return _maxThrust; }
                if (_speedWayAboveTarget.Contains(sr)) { return Constants.ZeroF; }
                return Constants.ZeroF;
            }
        }
    }
}

