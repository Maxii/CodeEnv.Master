// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipNavigator.cs
// Ship navigator and autopilot.
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
    /// Ship navigator and autopilot.
    /// </summary>
    public class ShipNavigator : ANavigator {

        /// <summary>
        /// The SQRD distance from the target that is 'close enough' to have arrived. This value
        /// is automatically adjusted to accomodate the radius of the target since all distance 
        /// calculations use the target's center point as its position.
        /// </summary>
        private float _closeEnoughDistanceToTargetSqrd;
        private float _closeEnoughDistanceToTarget;
        /// <summary>
        /// The distance from the target that is 'close enough' to have arrived. This value
        /// is automatically adjusted to accomodate the radius of the target since all distance 
        /// calculations use the target's center point as its position.
        /// </summary>
        private float CloseEnoughDistanceToTarget {
            get { return _closeEnoughDistanceToTarget; }
            set {
                _closeEnoughDistanceToTarget = Target.Radius + value;
                _closeEnoughDistanceToTargetSqrd = _closeEnoughDistanceToTarget * _closeEnoughDistanceToTarget;
            }
        }

        protected override Vector3 Destination {
            get { return Target.Position + Data.FormationStation.StationOffset; }
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

        private bool _isDetachedDuty;
        private IShipModel _ship;
        private GameStatus _gameStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipNavigator" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        public ShipNavigator(IShipModel ship)
            : base(ship.Data) {
            _ship = ship;
            _gameStatus = GameStatus.Instance;
            Subscribe();
        }

        protected override void Subscribe() {
            base.Subscribe();
            _subscribers.Add(Data.SubscribeToPropertyChanged<ShipData, float>(d => d.MaxWeaponsRange, OnWeaponsRangeChanged));
            _subscribers.Add(Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeed, OnFullSpeedChanged));
        }

        /// <summary>
        /// Plots a course and notifies the requester of the outcome via the onCoursePlotCompleted event if set.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="standoffDistance">The distance to standoff from the target. This is added to the radius of the target to
        /// determine how close the ship is allowed to approach the target.</param>
        /// <param name="isDetachedDuty">if set to <c>true</c> this navigator will ignore any fleetCmd restrictions.</param>
        public void PlotCourse(IDestinationTarget target, Speed speed, float standoffDistance, bool isDetachedDuty) {
            Target = target;
            Speed = speed;
            D.Assert(speed != Speed.AllStop, "Designated speed to new target {0} is 0!".Inject(target.FullName));
            CloseEnoughDistanceToTarget = standoffDistance;
            _isDetachedDuty = isDetachedDuty;
            InitializeTargetValues();
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
        /// Engages pilot execution of a direct homing course to the Target. No A* course is used.
        /// </summary>
        /// <returns></returns>
        private IEnumerator EngageHomingCourseToTarget() {
            //D.Log("Initiating coroutine for approach to {0}.", Destination);
            Vector3 newHeading = (Destination - Data.Position).normalized;
            if (!newHeading.IsSameDirection(Data.RequestedHeading, 0.1F)) {
                _ship.ChangeHeading(newHeading, isAutoPilot: true);
            }

            int courseCheckPeriod = _courseHeadingCheckPeriod;
            bool isSpeedIncreaseMade = false;

            float distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - Data.Position);
            float previousDistanceSqrd = distanceToDestinationSqrd;

            while (distanceToDestinationSqrd > _closeEnoughDistanceToTargetSqrd) {
                //D.Log("{0} distance to {1} = {2}.", Data.FullName, Target.FullName, Mathf.Sqrt(distanceToDestinationSqrd));
                if (!isSpeedIncreaseMade) {    // adjusts speed as a oneshot until we get there
                    isSpeedIncreaseMade = IncreaseSpeedOnHeadingConfirmation();
                }
                Vector3 correctedHeading;
                if (CheckForCourseCorrection(distanceToDestinationSqrd, out correctedHeading, ref courseCheckPeriod)) {
                    D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", Data.FullName, Vector3.Angle(correctedHeading, Data.RequestedHeading));
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
            Speed turnSpeed = Speed;    // TODO slow for the turn?
            _ship.ChangeSpeed(turnSpeed, isAutoPilot: true);
            _ship.ChangeHeading(newHeading, isAutoPilot: true);
        }

        /// <summary>
        /// Increases the speed of the ship when both the ship and the flagship 
        /// have achieved the requested heading.
        /// </summary>
        /// <returns><c>true</c> if the heading is confirmed and speed changed.</returns>
        private bool IncreaseSpeedOnHeadingConfirmation() {
            D.Log("{0}.IsTurning = {1}, IsDetachedDuty = {2}. {3}.IsTurning = {4}.",
    _ship.FullName, _ship.IsTurning, _isDetachedDuty, _ship.Command.HQElement.FullName, _ship.Command.HQElement.IsTurning);
            if (!_ship.IsTurning && !(_ship.Command.HQElement.IsTurning && !_isDetachedDuty)) {
                //D.Log("{0}.IsHeadingConfirmed = {1}, IsDetachedDuty = {2}. {3}.IsHeadingConfirmed = {4}.",
                //    _ship.FullName, _ship.IsHeadingConfirmed, _isDetachedDuty, _ship.Command.HQElement.FullName, _ship.Command.HQElement.IsHeadingConfirmed);
                //if (_ship.IsHeadingConfirmed && !(!_ship.Command.HQElement.IsHeadingConfirmed && !_isDetachedDuty)) {
                //if (Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, 1F)) {
                // we are close to being on course, so increase speed from orders
                _ship.ChangeSpeed(Speed, isAutoPilot: true);
                D.Log("{0} increasing speed on Heading Confirmation. Angle between current and requested heading is {1:0.0000}.",
                    _ship.FullName, Vector3.Angle(Data.CurrentHeading, Data.RequestedHeading));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks the course and provides any heading corrections needed.
        /// </summary>
        /// <param name="distanceToDestinationSqrd">The distance to destination SQRD.</param>
        /// <param name="correctedHeading">The corrected heading.</param>
        /// <param name="checkCount">The check count. When the value reaches 0, the course is checked.</param>
        /// <returns>true if a course correction to <c>correctedHeading</c> is needed.</returns>
        private bool CheckForCourseCorrection(float distanceToDestinationSqrd, out Vector3 correctedHeading, ref int checkCount) {
            if (distanceToDestinationSqrd < _courseHeadingCheckDistanceThresholdSqrd) {
                checkCount = 0;
            }
            if (checkCount == 0) {
                // check the course
                //D.Log("{0} is attempting to check its course. IsTurnComplete = {1}.", Data.Name, IsTurnComplete);
                if (!_ship.IsTurning) {
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

        protected override void OnFullSpeedChanged() {
            base.OnFullSpeedChanged();
            InitializeTargetValues();
        }

        protected override void OnGameSpeedChanged() {
            base.OnGameSpeedChanged();
            InitializeTargetValues();
        }

        /// <summary>
        /// Initializes the values that depend on the target and speed.
        /// </summary>
        protected override void InitializeTargetValues() {
            float speedFactor = Data.FullSpeed * _gameSpeedMultiplier * 3F;
            __separationTestToleranceDistanceSqrd = speedFactor * speedFactor;   // FIXME needs work - courseUpdatePeriod???
            //D.Log("{0} SeparationToleranceSqrd = {1}, FullSpeed = {2}.", Data.FullName, __separationTestToleranceDistanceSqrd, Data.FullSpeed);

            _courseHeadingCheckPeriod = Mathf.RoundToInt(1000 / (speedFactor * 5));  // higher speeds mean a shorter period between course checks, aka more frequent checks
            _courseHeadingCheckDistanceThresholdSqrd = speedFactor * speedFactor;   // higher speeds mean course checks become continuous further away
            if (Target != null && !Target.IsMovable) {  // target can be null
                // the target doesn't move so course checks are much less important
                _courseHeadingCheckPeriod *= 5;
                _courseHeadingCheckDistanceThresholdSqrd /= 5F;
            }
            _courseHeadingCheckDistanceThresholdSqrd = Mathf.Max(_courseHeadingCheckDistanceThresholdSqrd, _closeEnoughDistanceToTargetSqrd * 2);
            //D.Log("{0}: CourseCheckPeriod = {1}, CourseCheckDistanceThreshold = {2}.", Data.FullName, _courseHeadingCheckPeriod, Mathf.Sqrt(_courseHeadingCheckDistanceThresholdSqrd));
        }

        /// <summary>
        /// Checks whether the pilot can approach the provided location directly.
        /// </summary>
        /// <param name="location">The location to approach.</param>
        /// <returns>
        ///   <c>true</c> if there is nothing obstructing a direct approach.
        /// </returns>
        private bool CheckApproachTo(Vector3 location) {
            Vector3 currentPosition = Data.Position;
            Vector3 vectorToLocation = location - currentPosition;
            float distanceToLocation = vectorToLocation.magnitude;
            if (distanceToLocation < CloseEnoughDistanceToTarget) {
                // already inside close enough distance
                return true;
            }
            Vector3 directionToLocation = vectorToLocation.normalized;
            float rayDistance = distanceToLocation - CloseEnoughDistanceToTarget;
            float clampedRayDistance = Mathf.Clamp(rayDistance, 0.1F, Mathf.Infinity);
            RaycastHit hitInfo;
            if (Physics.Raycast(currentPosition, directionToLocation, out hitInfo, clampedRayDistance, _keepoutOnlyLayerMask.value)) {
                D.Log("{0} encountered obstacle {1} when checking approach to {2}.", Data.FullName, hitInfo.collider.name, location);
                // there is a keepout zone obstacle in the way 
                return false;
            }
            return true;
        }


        /// <summary>
        /// Checks whether the distance between 2 objects is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestinationSqrd">The distance automatic current destination SQRD.</param>
        /// <param name="previousDistanceSqrd">The previous distance SQRD.</param>
        /// <returns>true if the seperation distance is increasing.</returns>
        private bool CheckSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
            if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + __separationTestToleranceDistanceSqrd) {
                D.Warn("{0} separating from {1}. DistanceSqrd = {2}, previousSqrd = {3}, tolerance = {4}.",
    Data.FullName, Target.FullName, distanceToCurrentDestinationSqrd, previousDistanceSqrd, __separationTestToleranceDistanceSqrd);
                return true;
            }
            if (distanceToCurrentDestinationSqrd < previousDistanceSqrd) {
                // while we continue to move closer to the current destination, keep previous distance current
                // once we start to move away, we must not update it if we want the tolerance check to catch it
                previousDistanceSqrd = distanceToCurrentDestinationSqrd;
            }
            return false;
        }

        protected override void AssessFrequencyOfCourseProgressChecks() {
            // frequency of course progress checks increases as fullSpeed and gameSpeed increase
            float courseProgressCheckFrequency = 1F + (Data.FullSpeed * _gameSpeedMultiplier);
            _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
            D.Log("{0} frequency of course progress checks adjusted to {1:0.####}.", Data.FullName, courseProgressCheckFrequency);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

