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

//#define DEBUG_LOG
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

        protected override Vector3 Destination {
            get { return Target.Position + Data.FormationPosition; }
        }

        private bool IsTurnComplete {
            get {
                //D.Log("{0} heading passing {1} toward {2}.", _data.Name, _data.CurrentHeading, _data.RequestedHeading);
                return Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, 0.1F);
            }
        }

        protected new ShipData Data { get { return base.Data as ShipData; } }

        /// <summary>
        /// The number of update cycles between course heading checks while the target is
        /// beyond the _courseCheckDistanceThreshold.
        /// </summary>
        private int _courseHeadingCheckPeriod;

        /// <summary>
        /// The _course check distance threshold SQRD
        /// </summary>
        private float _courseHeadingCheckDistanceThresholdSqrd;

        /// <summary>
        /// The tolerance value used to test whether separation between 2 items is increasing. This 
        /// is a squared value.
        /// </summary>
        private float __separationTestToleranceDistanceSqrd;


        private Transform _transform;
        private GameStatus _gameStatus;
        private Job _headingJob;
        private EngineRoom _engineRoom;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipNavigator"/> class.
        /// </summary>
        /// <param name="t">Ship Transform</param>
        /// <param name="data">Ship data.</param>
        public ShipNavigator(Transform t, ShipData data)
            : base(data) {
            _transform = t;
            _gameStatus = GameStatus.Instance;
            _engineRoom = new EngineRoom(data, t.rigidbody);
            Subscribe();
        }

        protected override void Subscribe() {
            base.Subscribe();
            _subscribers.Add(Data.SubscribeToPropertyChanged<ShipData, float>(d => d.WeaponRange, OnWeaponsRangeChanged));
        }

        /// <summary>
        /// Plots a direct course to the target and notifies the ship of the outcome via the
        /// onCoursePlotSuccess/Failure events if set. The actual location is adjusted for the ship's
        /// position within the fleet's formation.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        public override void PlotCourse(IDestinationTarget target, float speed) {
            base.PlotCourse(target, speed);
            if (CheckApproachTo(Destination)) {
                OnCoursePlotSuccess();
            }
            else {
                OnCoursePlotFailure();
            }
        }

        /// <summary>
        /// Engages pilot execution to destination by direct
        /// approach. A ship does not use A* pathing.
        /// </summary>
        public override void Engage() {
            base.Engage();
            _pilotJob = new Job(EngageHomingCourseToTarget(), true);
        }

        /// <summary>
        /// Changes the direction the ship is headed in normalized world space coordinates.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="isManualOverride">if set to <c>true</c> [is manual override].</param>
        /// <returns>
        ///   <c>true</c> if the command was accepted, <c>false</c> if the command is a duplicate.
        /// </returns>
        public bool ChangeHeading(Vector3 newHeading, bool isManualOverride = true) {
            if (DebugSettings.Instance.StopShipMovement) {
                Disengage();
                return false;
            }
            if (isManualOverride) {
                Disengage();
            }

            newHeading.ValidateNormalized();
            if (newHeading.IsSameDirection(Data.RequestedHeading, 0.1F)) {
                D.Warn("Duplicate ChangeHeading Command to {0} on {1}.", newHeading, Data.Name);
                return false;
            }
            Data.RequestedHeading = newHeading;
            if (_headingJob != null && _headingJob.IsRunning) {
                _headingJob.Kill();
            }
            _headingJob = new Job(ExecuteHeadingChange(), toStart: true, onJobComplete: (wasKilled) => {
                if (wasKilled && !_isDisposing) {
                    D.Log("{0} turn command cancelled. Current Heading is {1}.", Data.Name, Data.CurrentHeading);
                }
                else {
                    D.Log("Turn complete. {0} current heading is {1}.", Data.Name, Data.CurrentHeading);
                }
            });
            return true;
        }

        /// <summary>
        /// Changes the speed of the ship.
        /// </summary>
        /// <param name="newSpeedRequest">The new speed request.</param>
        /// <param name="isManualOverride">if set to <c>true</c> [is manual override].</param>
        /// <returns></returns>
        public bool ChangeSpeed(float newSpeedRequest, bool isManualOverride = true) {
            if (DebugSettings.Instance.StopShipMovement) {
                Disengage();
                return false;
            }
            if (isManualOverride) {
                Disengage();
            }

            return _engineRoom.ChangeSpeed(newSpeedRequest);
        }

        /// <summary>
        /// Engages pilot execution of a direct homing course to the Target. No A* course is used.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageHomingCourseToTarget() {
            //D.Log("Initiating coroutine for approach to {0}.", Destination);
            Vector3 newHeading = (Destination - Data.Position).normalized;
            ChangeHeading(newHeading, isManualOverride: false);

            int courseCheckPeriod = _courseHeadingCheckPeriod;
            bool isSpeedIncreaseMade = false;

            float distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - Data.Position);
            float previousDistanceSqrd = distanceToDestinationSqrd;

            while (distanceToDestinationSqrd > _desiredDistanceFromTargetSqrd) {
                D.Log("Distance to {0} = {1}.", Target.Name, Mathf.Sqrt(distanceToDestinationSqrd));
                if (!isSpeedIncreaseMade) {    // adjusts speed as a oneshot until we get there
                    isSpeedIncreaseMade = IncreaseSpeedOnHeadingConfirmation();
                }
                Vector3 correctedHeading;
                if (CheckForCourseCorrection(distanceToDestinationSqrd, out correctedHeading, ref courseCheckPeriod)) {
                    D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", Data.Name, Vector3.Angle(correctedHeading, Data.RequestedHeading));
                    AdjustHeadingAndSpeedForTurn(correctedHeading);
                    isSpeedIncreaseMade = false;
                }
                if (CheckSeparation(distanceToDestinationSqrd, ref previousDistanceSqrd)) {
                    // we've missed the target or its getting away
                    OnCourseTrackingError();
                    yield break;
                }
                distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - Data.Position);
                yield return new WaitForSeconds(_courseProgressCheckPeriod);
            }

            OnDestinationReached();
        }

        private void AdjustHeadingAndSpeedForTurn(Vector3 newHeading) {
            float turnSpeed = Speed;
            ChangeSpeed(turnSpeed, isManualOverride: false); // slow for the turn
            ChangeHeading(newHeading, isManualOverride: false);
        }

        /// <summary>
        /// Increases the speed of the fleet when the correct heading has been achieved.
        /// </summary>
        /// <returns><c>true</c> if the heading is confirmed and speed changed.</returns>
        private bool IncreaseSpeedOnHeadingConfirmation() {
            if (Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, 1F)) {
                // we are close to being on course, so increase speed from orders
                ChangeSpeed(Speed, isManualOverride: false);
                //D.Log("At Heading Confirmation, angle between current and requested heading = {0:0.00}.", Vector3.Angle(Data.CurrentHeading, Data.RequestedHeading));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks the course and makes any heading corrections needed.
        /// </summary>
        /// <param name="distanceToDestinationSqrd"></param>
        /// <param name="checkCount">The check count. When the value reaches 0, the course is checked.</param>
        private bool CheckForCourseCorrection(float distanceToDestinationSqrd, out Vector3 correctedHeading, ref int checkCount) {
            if (distanceToDestinationSqrd < _courseHeadingCheckDistanceThresholdSqrd) {
                checkCount = 0;
            }
            if (checkCount == 0) {
                // check the course
                //D.Log("{0} is attempting to check its course. IsTurnComplete = {1}.", Data.Name, IsTurnComplete);
                if (IsTurnComplete) {
                    Vector3 testHeading = (Destination - Data.Position);
                    if (!testHeading.IsSameDirection(Data.RequestedHeading, 1F)) {
                        correctedHeading = testHeading.normalized;
                        return true;
                    }
                }
                checkCount = _courseHeadingCheckPeriod;
            }
            else {
                checkCount--;
            }
            correctedHeading = Vector3.zero;
            return false;
        }

        /// <summary>
        /// Initializes the values that depend on the target and speed.
        /// </summary>
        protected override void InitializeTargetValues() {
            float speedFactor = Speed * 3F;

            __separationTestToleranceDistanceSqrd = speedFactor * speedFactor;   // FIXME needs work - courseUpdatePeriod???

            DesiredDistanceFromTarget = Target.Radius + Data.WeaponRange;
            _courseHeadingCheckPeriod = Mathf.RoundToInt(1000 / (speedFactor * 5));  // higher speeds mean fewer periods between course checks, aka more frequent checks
            _courseHeadingCheckDistanceThresholdSqrd = speedFactor * speedFactor;   // higher speeds mean course checks become continuous further away
            if (!Target.IsMovable) {
                // the target doesn't move so course checks are much less important
                _courseHeadingCheckPeriod *= 5;
                _courseHeadingCheckDistanceThresholdSqrd /= 5F;
            }
            _courseHeadingCheckDistanceThresholdSqrd = Mathf.Max(_courseHeadingCheckDistanceThresholdSqrd, _desiredDistanceFromTargetSqrd * 2);
            D.Log("{0}: CourseCheckPeriod = {1}, CourseCheckDistanceThreshold = {2}.", Data.Name, _courseHeadingCheckPeriod, Mathf.Sqrt(_courseHeadingCheckDistanceThresholdSqrd));
        }

        /// <summary>
        /// Coroutine that executes a heading change. 
        /// </summary>
        /// <returns></returns>
        private IEnumerator ExecuteHeadingChange() {
            int previousFrameCount = Time.frameCount - 1;   // FIXME makes initial framesSinceLastPass = 1

            float maxRadianTurnRatePerSecond = Mathf.Deg2Rad * Data.MaxTurnRate * (GameDate.HoursPerSecond / GameDate.HoursPerDay);
            //D.Log("New coroutine. {0} coming to heading {1} at {2} radians/day.", _data.Name, _data.RequestedHeading, _data.MaxTurnRate);
            while (!IsTurnComplete) {
                int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
                previousFrameCount = Time.frameCount;
                float allowedTurn = maxRadianTurnRatePerSecond * GameTime.DeltaTimeOrPausedWithGameSpeed * framesSinceLastPass;
                Vector3 newHeading = Vector3.RotateTowards(Data.CurrentHeading, Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
                // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
                //D.Log("AllowedTurn = {0:0.0000}, CurrentHeading = {1}, ReqHeading = {2}, NewHeading = {3}", allowedTurn, Data.CurrentHeading, Data.RequestedHeading, newHeading);
                _transform.rotation = Quaternion.LookRotation(newHeading);
                yield return null; // new WaitForSeconds(0.5F);
            }
        }

        /// <summary>
        /// Checks whether the distance between 2 objects is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestinationSqrd">The distance automatic current destination SQRD.</param>
        /// <param name="previousDistanceSqrd">The previous distance SQRD.</param>
        /// <returns>true if the seperation distance is increasing.</returns>
        protected bool CheckSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
            if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + __separationTestToleranceDistanceSqrd) {
                D.Warn("{0} separating from {1}. DistanceSqrd = {2}, previousSqrd = {3}, tolerance = {4}.",
    Data.Name, Target.Name, distanceToCurrentDestinationSqrd, previousDistanceSqrd, __separationTestToleranceDistanceSqrd);
                return true;
            }
            if (distanceToCurrentDestinationSqrd < previousDistanceSqrd) {
                // while we continue to move closer to the current destination, keep previous distance current
                // once we start to move away, we must not update it if we want the tolerance check to catch it
                previousDistanceSqrd = distanceToCurrentDestinationSqrd;
            }
            return false;
        }


        protected override void Cleanup() {
            base.Cleanup();
            if (_headingJob != null) {
                _headingJob.Kill();
            }
            _engineRoom.Dispose();
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

