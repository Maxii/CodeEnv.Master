// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipNavigator.cs
// Ship navigator and pilot.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Ship navigator and pilot.
    /// </summary>
    public class ShipNavigator : ANavigator {

        /// <summary>
        /// Optional events for notification of the course plot being completed. 
        /// </summary>
        public event Action onCoursePlotFailure;
        public event Action onCoursePlotSuccess;

        /// <summary>
        /// Optional event for notification of destination reached.
        /// </summary>
        public event Action onDestinationReached;

        /// <summary>
        /// Optional event for notification of when the pilot heading for Destination
        /// detects an error while trying to get there. The principal error tested for is
        /// the ship missing the destination and continuing on.
        /// </summary>
        public event Action onCourseTrackingError;

        /// <summary>
        /// The number of times the heading and speed are checked between destinations.
        /// </summary>
        private int _courseCheckFrequency = 5;

        private Transform _transform;
        private Rigidbody _rigidbody;
        private GameStatus _gameStatus;
        private GeneralSettings _generalSettings;
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
        public ShipNavigator(Transform t, ShipData data)
            : base() {
            _transform = t;
            _data = data;
            _rigidbody = t.rigidbody;
            _rigidbody.useGravity = false;
            _gameStatus = GameStatus.Instance;
            _generalSettings = GeneralSettings.Instance;

            _thrustHelper = new ThrustHelper(0F, 0F, _data.MaxThrust);
            Subscribe();
        }

        protected override void Subscribe() {
            base.Subscribe();
            _subscribers.Add(_gameStatus.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsPaused, OnIsPausedChanged));
            onDestinationReached += OnDestinationReached;
            onCourseTrackingError += OnCourseTrackingError;
        }

        private void OnIsPausedChanged() {
            if (_gameStatus.IsPaused) {
                _velocityOnPause = _rigidbody.velocity;
                _rigidbody.isKinematic = true;
            }
            else {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = _velocityOnPause;
                _rigidbody.WakeUp();
            }
        }

        /// <summary>
        /// Plots a direct course to the provided destination and notifies the ship of the outcome via the 
        /// onCoursePlotSuccess/Failure events if set. The actual destination is adjusted for the ship's
        /// position within the fleet's formation.
        /// </summary>
        /// <param name="destination">The destination.</param>
        public override void PlotCourse(Vector3 destination) {
            Destination = destination + _data.FormationPosition;
            if (CheckDirectApproachToDestination()) {
                onCoursePlotSuccess();
            }
            else {
                onCoursePlotFailure();
            }
        }


        /// <summary>
        /// Engages pilot execution to destination by direct
        /// approach. A ship does not use A* pathing.
        /// </summary>
        public override void Engage() {
            base.Engage();
            _pilotJob = new Job(EngageDirectCourse(), true);
        }

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <returns><c>true</c> if the command was accepted, <c>false</c> if the command is a duplicate.</returns>
        public bool ChangeHeading(Vector3 newHeading, bool isManualOverride = true) {
            if (DebugSettings.Instance.StopShipMovement || isManualOverride) {
                Disengage();
            }

            newHeading.ValidateNormalized();
            if (Mathfx.Approx(newHeading, _data.RequestedHeading, 0.01F)) {
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

        public bool ChangeSpeed(float newSpeedRequest, bool isManualOverride = true) {
            if (DebugSettings.Instance.StopShipMovement || isManualOverride) {
                Disengage();
            }

            float newSpeed = Mathf.Clamp(newSpeedRequest, Constants.ZeroF, _data.MaxSpeed);
            float previousRequestedSpeed = _data.RequestedSpeed;
            float newSpeedToRequestedSpeedRatio = (previousRequestedSpeed != Constants.ZeroF) ? newSpeed / previousRequestedSpeed : Constants.ZeroF;
            if (ThrustHelper.SpeedTargetRange.Contains(newSpeedToRequestedSpeedRatio)) {
                D.Warn("{1} ChangeSpeed Command to {0} (Max = {2}) not executed. Target speed unchanged.", newSpeedRequest, _transform.name, _data.MaxSpeed);
                return false;
            }
            _data.RequestedSpeed = newSpeed;
            float thrustNeededToMaintainRequestedSpeed = newSpeed * _data.Mass * _data.Drag;
            _thrustHelper = new ThrustHelper(newSpeed, thrustNeededToMaintainRequestedSpeed, _data.MaxThrust);
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
        /// Engages autopilot execution of a direct path to the Destination. No A* course is used.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageDirectCourse() {
            D.Log("Initiating coroutine for approach to {0}.", Destination);

            Vector3 newHeading = (Destination - _data.Position).normalized;
            AdjustHeadingAndSpeedForTurn(newHeading);

            float initialDistanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _data.Position);
            int checksRemaining = _courseCheckFrequency - 1;

            bool isSpeedIncreaseMade = false;

            float distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _data.Position);
            float previousDistanceSqrd = distanceToDestinationSqrd;

            while (distanceToDestinationSqrd > _closeEnoughDistanceSqrd) {
                //D.Log("Distance to Destination = {0}.", distanceToDestination);
                if (!isSpeedIncreaseMade) {    // adjusts speed as a oneshot until we get there
                    isSpeedIncreaseMade = IncreaseSpeedOnHeadingConfirmation();
                }
                CheckCourse(Destination, initialDistanceToDestinationSqrd, ref checksRemaining);    // IMPROVE combine CheckCourse and CheckForError
                distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _data.Position);
                if (GameUtility.CheckForIncreasingSeparation(distanceToDestinationSqrd, ref previousDistanceSqrd)) {
                    //if (CheckForCourseTrackingError(distanceToDestinationSqrd, ref previousDistanceSqrd)) {
                    // we've missed the destination or its getting away
                    onCourseTrackingError();
                    yield break;
                }
                yield return new WaitForSeconds(_courseUpdatePeriod);
            }
            //D.Log("Final Approach coroutine ended.");
            onDestinationReached();
        }

        private void AdjustHeadingAndSpeedForTurn(Vector3 newHeading) {
            ChangeSpeed(0.1F, isManualOverride: false); // slow for the turn
            ChangeHeading(newHeading, isManualOverride: false);
        }

        /// <summary>
        /// Increases the speed of the fleet when the correct heading has been achieved.
        /// </summary>
        /// <returns><c>true</c> if the heading is confirmed and speed changed.</returns>
        private bool IncreaseSpeedOnHeadingConfirmation() {
            if (CodeEnv.Master.Common.Mathfx.Approx(_data.CurrentHeading, _data.RequestedHeading, .1F)) {
                // we are close to being on course, so punch it up to warp 9!
                ChangeSpeed(2.0F, isManualOverride: false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks the course and makes any heading corrections needed.
        /// </summary>
        /// <param name="destination">The current destination.</param>
        /// <param name="distanceBetweenDestinationsSqrd">The distance between destinations SQRD.</param>
        /// <param name="checksRemaining">The number of course checks remaining on this leg.</param>
        private void CheckCourse(Vector3 destination, float distanceBetweenDestinationsSqrd, ref int checksRemaining) {
            if (checksRemaining <= 0) {
                return;
            }
            float distanceToDestinationSqrd = Vector3.SqrMagnitude(destination - _data.Position);
            float nextCheckDistanceSqrd = distanceBetweenDestinationsSqrd * (checksRemaining / (float)_courseCheckFrequency);
            if (distanceToDestinationSqrd < nextCheckDistanceSqrd) {
                Vector3 newHeading = (destination - _data.Position).normalized;
                if (!CodeEnv.Master.Common.Mathfx.Approx(newHeading, _data.RequestedHeading, .01F)) {
                    //_fleet.ChangeFleetSpeed(0.1F, isManualOverride: false);
                    ChangeHeading(newHeading, isManualOverride: false);
                    D.Log("{0} has made a midcourse correction to {1} at checkpoint {2} of {3}.",
                        _data.Name, newHeading, checksRemaining, _courseCheckFrequency);
                }
                else {
                    D.Log("{0} has made a midcourse correction check with no change at checkpoint {1} of {2}.",
                        _data.Name, checksRemaining, _courseCheckFrequency);
                }
                checksRemaining--;
            }
        }

        /// <summary>
        /// Checks whether the pilot can approach the Destination directly.
        /// </summary>
        /// <returns><c>true</c> if a direct approach is feasible.</returns>
        private bool CheckDirectApproachToDestination() {
            Vector3 currentPosition = _data.Position;
            Vector3 directionToDestination = (Destination - currentPosition).normalized;
            float distanceToDestination = Vector3.Distance(currentPosition, Destination);
            if (Physics.Raycast(currentPosition, directionToDestination, distanceToDestination)) {
                D.Log("Obstacle encountered when checking approach to Destination.");
                // there is an obstacle in the way so continue to follow the course
                return false;
            }
            return true;
        }


        /// <summary>
        /// Adjusts the velocity and thrust of the ship to reflect the new GameClockSpeed setting. 
        /// The reported speed and directional heading of the ship is not affected.
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        protected override void AdjustForGameSpeed(float gameSpeedChangeRatio) {
            base.AdjustForGameSpeed(gameSpeedChangeRatio);
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

        protected override void Cleanup() {
            base.Cleanup();
            if (_headingJob != null) {
                _headingJob.Kill();
            }
            if (_speedJob != null) {
                _speedJob.Kill();
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        public class ThrustHelper {

            public static Range<float> SpeedTargetRange = new Range<float>(0.99F, 1.01F);

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

