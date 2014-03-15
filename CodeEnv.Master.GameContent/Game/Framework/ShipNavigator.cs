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

//#define DEBUG_LOG
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

        protected override Vector3 Destination {
            get { return Target.Position + Data.FormationStationOffset; }
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

        private IShipNavigatorClient _ship;
        private GameStatus _gameStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipNavigator"/> class.
        /// </summary>
        /// <param name="t">Ship Transform</param>
        /// <param name="data">Ship data.</param>
        public ShipNavigator(IShipNavigatorClient ship)
            : base(ship.Data) {
            _ship = ship;
            _gameStatus = GameStatus.Instance;
            Subscribe();
        }

        protected override void Subscribe() {
            base.Subscribe();
            //_subscribers.Add(Data.SubscribeToPropertyChanged<ShipData, float>(d => d.WeaponRange, OnWeaponsRangeChanged));
            _subscribers.Add(Data.SubscribeToPropertyChanged<ShipData, float>(d => d.MaxWeaponsRange, OnWeaponsRangeChanged));
            _subscribers.Add(Data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeed, OnFullSpeedChanged));
        }

        /// <summary>
        /// Plots a course and notifies the requester of the outcome via the onCoursePlotCompleted event if set.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="standoffDistance">The distance to standoff from the target.</param>
        public void PlotCourse(IDestinationItem target, Speed speed, float standoffDistance = Constants.ZeroF) {
            Target = target;
            Speed = speed;
            D.Assert(speed != Speed.AllStop, "Designated speed to new target {0} is 0!".Inject(target.Name));
            CloseEnoughDistanceToTarget = standoffDistance != Constants.ZeroF ? standoffDistance : Speed.Standard.GetValue(null, Data);
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
            Speed turnSpeed = Speed;
            _ship.ChangeSpeed(turnSpeed, isAutoPilot: true); // TODO slow for the turn?
            _ship.ChangeHeading(newHeading, isAutoPilot: true);
        }

        /// <summary>
        /// Increases the speed of the fleet when the correct heading has been achieved.
        /// </summary>
        /// <returns><c>true</c> if the heading is confirmed and speed changed.</returns>
        private bool IncreaseSpeedOnHeadingConfirmation() {
            if (Data.CurrentHeading.IsSameDirection(Data.RequestedHeading, 1F)) {
                // we are close to being on course, so increase speed from orders
                _ship.ChangeSpeed(Speed, isAutoPilot: true);
                //D.Log("At Heading Confirmation, angle between current and requested heading = {0:0.00}.", Vector3.Angle(Data.CurrentHeading, Data.RequestedHeading));
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
            D.Log("{0} SeparationToleranceSqrd = {1}, Data.FullSpeed = {2}.", Data.Name, __separationTestToleranceDistanceSqrd, Data.FullSpeed);

            _courseHeadingCheckPeriod = Mathf.RoundToInt(1000 / (speedFactor * 5));  // higher speeds mean a shorter period between course checks, aka more frequent checks
            _courseHeadingCheckDistanceThresholdSqrd = speedFactor * speedFactor;   // higher speeds mean course checks become continuous further away
            if (Target != null && !Target.IsMovable) {  // target can be null
                // the target doesn't move so course checks are much less important
                _courseHeadingCheckPeriod *= 5;
                _courseHeadingCheckDistanceThresholdSqrd /= 5F;
            }
            _courseHeadingCheckDistanceThresholdSqrd = Mathf.Max(_courseHeadingCheckDistanceThresholdSqrd, _closeEnoughDistanceToTargetSqrd * 2);
            D.Log("{0}: CourseCheckPeriod = {1}, CourseCheckDistanceThreshold = {2}.", Data.Name, _courseHeadingCheckPeriod, Mathf.Sqrt(_courseHeadingCheckDistanceThresholdSqrd));
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

        protected override void AssessFrequencyOfCourseProgressChecks() {
            // frequency of course progress checks increases as fullSpeed and gameSpeed increase
            float courseProgressCheckFrequency = 1F + (Data.FullSpeed * _gameSpeedMultiplier);
            _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
            D.Log("{0}.{1} frequency of course progress checks adjusted to {2:0.####}.", Data.Name, GetType().Name, courseProgressCheckFrequency);
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }
    }
}

