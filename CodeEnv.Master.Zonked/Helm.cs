// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Helm.cs
// A Ship's helm, encompassing both the auto pilot and navigator.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

//namespace CodeEnv.Master.GameContent {

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// A Ship's helm, encompassing both the auto pilot and navigator.
/// </summary>
public class Helm : APropertyChangeTracking, IDisposable {

    private class TargetInfo {

        /// <summary>
        /// The target this navigator is trying to reach. Can be a FormationStation, 
        /// StationaryLocation, UnitCommand, UnitElement or other MortalItem.
        /// </summary>
        public IDestinationTarget Target { get; private set; }

        /// <summary>
        /// The actual worldspace location this navigator is trying to reach, derived
        /// from the Target. Can be offset from the actual Target position by the
        /// ship's formation station offset.
        /// </summary>
        /// <value>
        /// The destination.
        /// </value>
        public Vector3 Destination { get { return Target.Position + _fstOffset; } }

        /// <summary>
        /// The distance from the Destination that is 'close enough' to have arrived. This value
        /// is automatically adjusted to accomodate various factors including the radius of the target 
        /// and any desired standoff distance.
        /// </summary>
        public float CloseEnoughDistance { get; private set; }

        /// <summary>
        /// The SQRD distance from the Destination that is 'close enough' to have arrived. This value
        /// is automatically adjusted to accomodate various factors including the radius of the target 
        /// and any desired standoff distance.
        /// </summary>
        public float CloseEnoughDistanceSqrd { get; private set; }

        private Vector3 _fstOffset;

        public TargetInfo(IFormationStation fst) {
            Target = fst as IDestinationTarget;
            //Destination = Target.Position;
            _fstOffset = Vector3.zero;
            CloseEnoughDistance = Constants.ZeroF;
            CloseEnoughDistanceSqrd = CloseEnoughDistance * CloseEnoughDistance;
        }

        public TargetInfo(StationaryLocation sl, Vector3 fstOffset, float fullSpeed) {
            Target = sl;
            //Destination = sl.Position + fstOffset;
            _fstOffset = fstOffset;
            CloseEnoughDistance = fullSpeed;    // FIXME this value does not get adjusted when OnFullSpeedChanged() occurs
            CloseEnoughDistanceSqrd = CloseEnoughDistance * CloseEnoughDistance;
        }

        public TargetInfo(ICommandTarget cmd, Vector3 fstOffset, float standoffDistance) {
            Target = cmd;
            //Destination = cmd.Position + fstOffset;
            _fstOffset = fstOffset;
            CloseEnoughDistance = cmd.Radius + standoffDistance;
            CloseEnoughDistanceSqrd = CloseEnoughDistance * CloseEnoughDistance;
        }

        public TargetInfo(IElementTarget element, float standoffDistance) {
            Target = element;
            //Destination = element.Position;
            _fstOffset = Vector3.zero;
            CloseEnoughDistance = element.Radius + standoffDistance;
            CloseEnoughDistanceSqrd = CloseEnoughDistance * CloseEnoughDistance;
        }

        public TargetInfo(IMortalTarget planetoid, Vector3 fstOffset, float standoffDistance) {
            Target = planetoid;
            //Destination = planetoid.Position + fstOffset;
            _fstOffset = fstOffset;
            CloseEnoughDistance = planetoid.Radius + standoffDistance;      // TODO should account for keepoutRadius
            CloseEnoughDistanceSqrd = CloseEnoughDistance * CloseEnoughDistance;
        }

        // TODO Star and System should be acceptable targets too
    }

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
    /// Optional event for notification of when the pilot 
    /// detects an error while trying to get to the target. 
    /// </summary>
    public event Action onDestinationUnreachable;

    /// <summary>
    /// The speed to travel at.
    /// </summary>
    public Speed Speed { get; private set; }

    public string TargetName {
        get {
            if (_targetInfo == null) {
                return "No Target";
            }
            return _targetInfo.Target.FullName;
        }
    }

    public bool IsBearingConfirmed { get; private set; }
    //public bool IsBearingConfirmed { get { return _data.CurrentHeading.IsSameDirection(_data.RequestedHeading, 1F); } }         // always accurate but expensive


    public bool IsAutoPilotEngaged {
        get { return _pilotJob != null && _pilotJob.IsRunning; }
    }

    private static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

    /// <summary>
    /// The number of course progress assessments allowed between course correction checks 
    /// while the target is beyond the _courseCorrectionCheckDistanceThreshold.
    /// </summary>
    private int _courseCorrectionCheckCountSetting;

    /// <summary>
    /// The (sqrd) distance threshold from the target where the course correction check
    /// frequency is determined by the _courseCorrectionCheckCountSetting. Once inside
    /// this distance threshold, course correction checks occur every time course progress is
    /// assessed.
    /// </summary>
    private float _courseCorrectionCheckDistanceThresholdSqrd;

    /// <summary>
    /// The tolerance value used to test whether separation between 2 items is increasing. This 
    /// is a squared value.
    /// </summary>
    private float __separationTestToleranceDistanceSqrd;

    /// <summary>
    /// Indicates whether the movement of the ship is constrained by fleet coordination requirements.
    /// Initially, if true, this means the ship does not depart until the fleet is ready.
    /// </summary>
    private bool _isFleetMove;

    /// <summary>
    /// The duration in seconds between course progress assessments. The default is
    /// every second at a speed of 1 unit per day and normal gamespeed.
    /// </summary>
    private float _courseProgressCheckPeriod = 1F;

    private float _waypointCloseEnoughDistance;
    private float _waypointCloseEnoughDistanceSqrd;

    private TargetInfo _targetInfo;
    private ShipData _data;
    private IShipModel _ship;
    private EngineRoom _engineRoom;
    private Rigidbody _rigidbody;

    private Job _pilotJob;
    private Job _headingJob;

    private IList<IDisposable> _subscribers;
    private GameStatus _gameStatus;
    private GameTime _gameTime;
    private float _gameSpeedMultiplier;

    /// <summary>
    /// Initializes a new instance of the <see cref="Helm" /> class.
    /// </summary>
    /// <param name="ship">The ship.</param>
    public Helm(IShipModel ship) {
        _ship = ship;
        _data = ship.Data;
        _rigidbody = ship.Transform.rigidbody;
        _gameTime = GameTime.Instance;
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
        _gameStatus = GameStatus.Instance;
        _engineRoom = new EngineRoom(_data, _rigidbody);
        AssessFrequencyOfCourseProgressChecks();
        Subscribe();
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        _subscribers.Add(_data.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeed, OnFullSpeedChanged));
    }

    #region PlotCourse

    /// <summary>
    /// Plots the course to the target and notifies the requester of the outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="speed">The speed.</param>
    /// <param name="standoffDistance">The distance to standoff from the target. When appropriate, this is added to the radius of the target to
    /// determine how close the ship is allowed to approach.</param>
    /// <param name="isFleetMove">if set to <c>true</c> the ship will only move when the fleet is ready.</param>
    public void PlotCourse(IDestinationTarget target, Speed speed, float standoffDistance, bool isFleetMove) {
        D.Assert(speed != default(Speed) && speed != Speed.AllStop, "{0} speed of {1} is illegal.".Inject(_ship.FullName, speed.GetName()));
        bool targetIsUnreachable = false;
        if (targetIsUnreachable) {
            // TODO are there any circumstances where the target is known to be unreachable when 'plotting the course'?
            OnCoursePlotFailure();
            return;
        }
        if (target is IFormationStation) {
            PlotCourse(target as IFormationStation, speed);
        }
        else if (target is StationaryLocation) {
            PlotCourse(target as StationaryLocation, speed, isFleetMove);
        }
        else if (target is ICommandTarget) {
            PlotCourse(target as ICommandTarget, speed, standoffDistance);
        }
        else if (target is IElementTarget) {
            PlotCourse(target as IElementTarget, speed, standoffDistance);
        }
        else if (target is IMortalTarget) {
            D.Assert(target is IPlanetoidModel);
            PlotCourse(target as IMortalTarget, speed, standoffDistance, isFleetMove);
        }
        else {
            D.Error("{0} of Type {1} not anticipated.", target.FullName, target.GetType().Name);
            return;
        }
        OnCoursePlotSuccess();
    }

    /// <summary>
    /// Plots a course to the ship's FormationStation and notifies the requester of the
    /// outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="station">The formation station.</param>
    /// <param name="speed">The speed.</param>
    private void PlotCourse(IFormationStation station, Speed speed) {
        _targetInfo = new TargetInfo(station);
        Speed = speed;
        _isFleetMove = false;
        InitializeTargetValues();
        //PlotCourse();
    }

    /// <summary>
    /// Plots a course to a stationary location and notifies the requester of the 
    /// outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="location">The stationary location.</param>
    /// <param name="speed">The speed.</param>
    /// <param name="isFleetMove">if set to <c>true</c> this navigator will only move when the fleet is ready.</param>
    private void PlotCourse(StationaryLocation location, Speed speed, bool isFleetMove) {
        // a formationOffset is required if this is a fleet move
        Vector3 destinationOffset = isFleetMove ? _data.FormationStation.StationOffset : Vector3.zero;
        _targetInfo = new TargetInfo(location, destinationOffset, _data.FullSpeed);
        Speed = speed;
        _isFleetMove = isFleetMove;
        InitializeTargetValues();
        //PlotCourse();
    }

    /// <summary>
    /// Plots a course to a Command target and notifies the requester of the 
    /// outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="cmd">The command target.</param>
    /// <param name="speed">The speed.</param>
    /// <param name="standoffDistance">The distance to standoff from the target. This is added to the radius of the target to
    /// determine how close the ship is allowed to approach the target.</param>
    private void PlotCourse(ICommandTarget cmd, Speed speed, float standoffDistance) {
        _targetInfo = new TargetInfo(cmd, _data.FormationStation.StationOffset, standoffDistance);
        Speed = speed;
        _isFleetMove = true;
        InitializeTargetValues();
        //PlotCourse();
    }

    /// <summary>
    /// Plots a course to a Element target and notifies the requester of the 
    /// outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="element">The element target.</param>
    /// <param name="speed">The speed.</param>
    /// <param name="standoffDistance">The distance to standoff from the target. This is added to the radius of the target to
    /// determine how close the ship is allowed to approach the target.</param>
    private void PlotCourse(IElementTarget element, Speed speed, float standoffDistance) {
        _targetInfo = new TargetInfo(element, standoffDistance);
        Speed = speed;
        _isFleetMove = false;
        InitializeTargetValues();
        //PlotCourse();
    }

    /// <summary>
    /// Plots a course to a Planetoid target and notifies the requester of the
    /// outcome via the onCoursePlotSuccess or Failure events.
    /// </summary>
    /// <param name="planetoid">The planetoid.</param>
    /// <param name="speed">The speed.</param>
    /// <param name="standoffDistance">The distance to standoff from the target. This is added to the radius of the target to
    /// determine how close the ship is allowed to approach the target.</param>
    /// <param name="isFleetMove">>if set to <c>true</c> this navigator will only move when the fleet is ready.</param>
    private void PlotCourse(IMortalTarget planetoid, Speed speed, float standoffDistance, bool isFleetMove) {
        // a formationOffset is required if this is a fleet move
        Vector3 destinationOffset = isFleetMove ? _data.FormationStation.StationOffset : Vector3.zero;
        _targetInfo = new TargetInfo(planetoid, destinationOffset, standoffDistance);
        Speed = speed;
        _isFleetMove = isFleetMove;
        InitializeTargetValues();
        //PlotCourse();
    }

    //private void PlotCourse() {
    //    InitializeTargetValues();
    //    if (!CheckApproachTo(_targetInfo.Destination)) {
    //        OnCoursePlotFailure();
    //        return;
    //    }
    //    OnCoursePlotSuccess();
    //}

    //private void PlotCourse() {
    //    InitializeTargetValues();
    //    //OnCoursePlotSuccess();
    //}

    #endregion

    /// <summary>
    /// Engages pilot execution to destination by direct
    /// approach. A ship does not use A* pathing.
    /// </summary>
    //public void EngageAutoPilot() {
    //    if (_pilotJob != null && _pilotJob.IsRunning) {
    //        _pilotJob.Kill();
    //    }
    //    _pilotJob = new Job(EngageDirectCourseToTarget(), true);
    //}

    public void EngageAutoPilot() {
        DisengageAutoPilot();
        if (CheckApproachTo(_targetInfo.Destination)) {
            InitiateDirectCourseToTarget();
        }
        else {
            InitiateCourseAroundObstacleTo(_targetInfo.Destination);
        }
    }


    /// <summary>
    /// Primary external control to disengage the pilot once Engage has been called.
    /// Does nothing if not already Engaged.
    /// </summary>
    public void DisengageAutoPilot() {
        if (IsAutoPilotEngaged) {
            D.Log("{0} AutoPilot disengaging.", _ship.FullName);
            _pilotJob.Kill();
        }
    }

    public void AlignBearingWithFlagship() {
        D.Log("{0} is aligning its bearing to {1}'s bearing {2}.", _ship.FullName, _ship.Command.HQElement.FullName, _ship.Command.HQElement.Data.RequestedHeading);
        ChangeHeading(_ship.Command.HQElement.Data.RequestedHeading);
        if (IsAutoPilotEngaged) {
            D.Warn("{0}.AutoPilot remains engaged.", _ship.FullName);
        }
    }

    /// <summary>
    /// Stops the ship. The ship will actually not stop instantly as it has
    /// momentum even with flaps deployed. Typically, this is called in the state
    /// machine after a Return() from the Moving state. Otherwise, the ship keeps
    /// moving in the direction and at the speed it had when it exited Moving.
    /// </summary>
    public void AllStop() {
        D.Log("{0}.AllStop() called.", _ship.FullName);

        ChangeSpeed(Speed.AllStop);
        if (IsAutoPilotEngaged) {
            D.Warn("{0}.AutoPilot remains engaged.", _ship.FullName);
        }
    }

    /// <summary>
    /// Changes the direction the ship is headed in normalized world space coordinates.
    /// </summary>
    /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    /// <returns><c>true</c> if the heading change was accepted.</returns>
    private bool ChangeHeading(Vector3 newHeading) {
        if (DebugSettings.Instance.StopShipMovement) {
            DisengageAutoPilot();
            return false;
        }

        newHeading.ValidateNormalized();
        if (newHeading.IsSameDirection(_data.RequestedHeading, 0.1F)) {
            D.Warn("{0} received a duplicate ChangeHeading Command to {1}.", _ship.FullName, newHeading);
            return false;
        }
        if (_headingJob != null && _headingJob.IsRunning) {
            _headingJob.Kill();
        }
        D.Log("{0} changing heading to {1}.", _ship.FullName, newHeading);
        _data.RequestedHeading = newHeading;
        IsBearingConfirmed = false;
        _headingJob = new Job(ExecuteHeadingChange(), toStart: true, onJobComplete: (jobWasKilled) => {
            if (!_isDisposing) {
                if (jobWasKilled) {
                    D.Log("{0}'s turn order to {1} has been cancelled.", _ship.FullName, _data.RequestedHeading);
                }
                else {
                    IsBearingConfirmed = true;
                    D.Log("{0}'s turn to {1} is complete.  Heading deviation is {2:0.00}.",
                        _ship.FullName, _data.RequestedHeading, Vector3.Angle(_data.CurrentHeading, _data.RequestedHeading));
                }
                // ExecuteHeadingChange() appeared to generate angular velocity which continued to turn the ship after the Job was complete.
                // The actual culprit was the physics engine which when started, found Creators had placed the non-kinematic ships at the same
                // location, relying on the formation generator to properly separate them later. The physics engine came on before the formation
                // had been deployed, resulting in both velocity and angular velocity from the collisions. The fix was to make the ship rigidbodies
                // kinematic until the formation had been deployed.
                //_rigidbody.angularVelocity = Vector3.zero;
            }
        });
        return true;
    }

    /// <summary>
    /// Coroutine that executes a heading change without overshooting.
    /// </summary>
    /// <returns></returns>
    private IEnumerator ExecuteHeadingChange() {
        int previousFrameCount = Time.frameCount - 1;   // FIXME makes initial framesSinceLastPass = 1

        float maxRadianTurnRatePerSecond = Mathf.Deg2Rad * _data.MaxTurnRate * (GameTime.HoursPerSecond / GameTime.HoursPerDay);
        //D.Log("New coroutine. {0} coming to heading {1} at {2} radians/day.", _data.Name, _data.RequestedHeading, _data.MaxTurnRate);
        while (!_data.CurrentHeading.IsSameDirection(_data.RequestedHeading, 1F)) {
            int framesSinceLastPass = Time.frameCount - previousFrameCount; // needed when using yield return WaitForSeconds()
            previousFrameCount = Time.frameCount;
            float allowedTurn = maxRadianTurnRatePerSecond * GameTime.DeltaTimeOrPausedWithGameSpeed * framesSinceLastPass;
            Vector3 newHeading = Vector3.RotateTowards(_data.CurrentHeading, _data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
            // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
            //D.Log("AllowedTurn = {0:0.0000}, CurrentHeading = {1}, ReqHeading = {2}, NewHeading = {3}", allowedTurn, Data.CurrentHeading, Data.RequestedHeading, newHeading);
            _ship.Transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on and off while rotating?
            //D.Log("{0} heading is now {1}.", FullName, Data.CurrentHeading);
            yield return null; // new WaitForSeconds(0.5F); // new WaitForFixedUpdate();
        }
    }

    /// <summary>
    /// Changes the speed of the ship.
    /// </summary>
    /// <param name="newSpeed">The new speed request.</param>
    /// <returns><c>true</c> if the speed change was accepted.</returns>
    private bool ChangeSpeed(Speed newSpeed) {
        if (DebugSettings.Instance.StopShipMovement) {
            DisengageAutoPilot();
            return false;
        }
        return _engineRoom.ChangeSpeed(newSpeed.GetValue(_ship.Command.Data, _data));
    }

    /// <summary>
    /// Engages pilot execution of a direct course to the Target. No A* course is used.
    /// </summary>
    /// <returns></returns>
    private IEnumerator EngageDirectCourseToTarget() {
        D.Log("{0} beginning prep for direct course to target {1}. Distance: {2}.",
            _ship.FullName, _targetInfo.Target.FullName, Vector3.Magnitude(_targetInfo.Destination - _data.Position));
        Vector3 newHeading = (_targetInfo.Destination - _data.Position).normalized;
        if (!newHeading.IsSameDirection(_data.RequestedHeading, 0.1F)) {
            ChangeHeading(newHeading);
        }
        if (_isFleetMove) {
            while (!_ship.Command.IsBearingConfirmed) {
                // wait here until the fleet is ready for departure
                yield return null;
            }
        }
        D.Log("Fleet has matched bearing. {0} powering up. Distance to target {1}: {2}.",
            _ship.FullName, _targetInfo.Target.FullName, Vector3.Magnitude(_targetInfo.Destination - _data.Position));

        int courseCorrectionCheckCountdown = _courseCorrectionCheckCountSetting;
        bool isSpeedChecked = false;

        float distanceToDestinationSqrd = Vector3.SqrMagnitude(_targetInfo.Destination - _data.Position);
        float previousDistanceSqrd = distanceToDestinationSqrd;

        while (distanceToDestinationSqrd > _targetInfo.CloseEnoughDistanceSqrd) {
            D.Log("{0} distance to {1} at {2} = {3}.", _ship.FullName, _targetInfo.Target.FullName, _targetInfo.Destination, Vector3.Magnitude(_targetInfo.Destination - _data.Position));
            if (!isSpeedChecked) {    // adjusts speed as a oneshot until we get there
                isSpeedChecked = AdjustSpeedOnHeadingConfirmation();
            }
            Vector3 correctedHeading;
            if (CheckForCourseCorrection(_targetInfo.Destination, distanceToDestinationSqrd, out correctedHeading, ref courseCorrectionCheckCountdown)) {
                D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", _ship.FullName, Vector3.Angle(correctedHeading, _data.RequestedHeading));
                AdjustHeadingAndSpeedForTurn(correctedHeading);
                isSpeedChecked = false;
            }

            if (!CheckApproachTo(_targetInfo.Destination)) {    // TODO make less frequent
                InitiateCourseAroundObstacleTo(_targetInfo.Destination);
            }

            if (CheckSeparation(distanceToDestinationSqrd, ref previousDistanceSqrd)) {
                // we've missed the target or its getting away
                OnDestinationUnreachable();
                yield break;
            }
            distanceToDestinationSqrd = Vector3.SqrMagnitude(_targetInfo.Destination - _data.Position);
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }
        OnDestinationReached();
    }

    private void AdjustHeadingAndSpeedForTurn(Vector3 newHeading) {
        //Speed turnSpeed = Speed;    // TODO slow for the turn?
        //_ship.ChangeSpeed(turnSpeed);
        ChangeHeading(newHeading);
    }

    /// <summary>
    /// Adjusts the speed of the ship (if needed) when the ship has finished its turn.
    /// </summary>
    /// <returns><c>true</c> if the heading was confirmed and speed checked.</returns>
    private bool AdjustSpeedOnHeadingConfirmation() {
        if (IsBearingConfirmed) {
            D.Log("{0} heading {1} is confirmed. Deviation is {2:0.00} degrees.", _ship.FullName, _data.RequestedHeading, Vector3.Angle(_data.CurrentHeading, _data.RequestedHeading));
            if (ChangeSpeed(Speed)) {
                D.Log("{0} adjusting speed to {1}. ", _ship.FullName, Speed.GetName());
            }
            else {
                D.Log("{0} continuing at speed of {1}.", _ship.FullName, Speed.GetName());
            }
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
    //private bool CheckForCourseCorrection(float distanceToDestinationSqrd, out Vector3 correctedHeading, ref int checkCount) {
    //    if (distanceToDestinationSqrd < _courseCorrectionCheckDistanceThresholdSqrd) {
    //        checkCount = 0;
    //    }
    //    if (checkCount == 0) {
    //        // check the course
    //        //D.Log("{0} is attempting to check its course.", Data.Name);
    //        if (IsBearingConfirmed) {
    //            Vector3 testHeading = (_targetInfo.Destination - _data.Position);
    //            if (!testHeading.IsSameDirection(_data.RequestedHeading, 1F)) {
    //                correctedHeading = testHeading.normalized;
    //                return true;
    //            }
    //        }
    //        checkCount = _courseCorrectionCheckCountSetting;
    //    }
    //    else {
    //        checkCount--;
    //    }
    //    correctedHeading = Vector3.zero;
    //    return false;
    //}

    /// <summary>
    /// Checks the course and provides any heading corrections needed.
    /// </summary>
    /// <param name="currentDestination">The current destination.</param>
    /// <param name="distanceToDestinationSqrd">The distance to destination SQRD.</param>
    /// <param name="correctedHeading">The corrected heading.</param>
    /// <param name="checkCount">The check count. When the value reaches 0, the course is checked.</param>
    /// <returns>
    /// true if a course correction to <c>correctedHeading</c> is needed.
    /// </returns>
    private bool CheckForCourseCorrection(Vector3 currentDestination, float distanceToDestinationSqrd, out Vector3 correctedHeading, ref int checkCount) {
        if (distanceToDestinationSqrd < _courseCorrectionCheckDistanceThresholdSqrd) {
            checkCount = 0;
        }
        if (checkCount == 0) {
            // check the course
            //D.Log("{0} is attempting to check its course.", Data.Name);
            if (IsBearingConfirmed) {
                Vector3 testHeading = (currentDestination - _data.Position);
                if (!testHeading.IsSameDirection(_data.RequestedHeading, 1F)) {
                    correctedHeading = testHeading.normalized;
                    return true;
                }
            }
            checkCount = _courseCorrectionCheckCountSetting;
        }
        else {
            checkCount--;
        }
        correctedHeading = Vector3.zero;
        return false;
    }


    private void OnCoursePlotFailure() {
        var temp = onCoursePlotFailure;
        if (temp != null) {
            temp();
        }
    }

    private void OnCoursePlotSuccess() {
        var temp = onCoursePlotSuccess;
        if (temp != null) {
            temp();
        }
    }

    /// <summary>
    /// Called when the ship gets 'close enough' to the destination
    /// EXCEPT when the destination is a formation station. In that case,
    /// closeEnoughDistance is 0 (radius and standoffDistance are not used)
    /// which means this event will never be raised. The actual arrival onStation
    /// is detected by the formation station itself which tells the ship's
    /// state machine, ending the move.
    /// </summary>
    private void OnDestinationReached() {
        //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
        D.Log("{0} at {1} reached {2} at {3} (w/station offset). Actual proximity {4:0.00} units.",
            _ship.FullName, _data.Position, _targetInfo.Target.FullName, _targetInfo.Destination, Vector3.Distance(_targetInfo.Destination, _data.Position));
        var temp = onDestinationReached;
        if (temp != null) {
            temp();
        }
    }

    private void OnDestinationUnreachable() {
        //_pilotJob.Kill(); // should be handled by the ship's state machine ordering a Disengage()
        var temp = onDestinationUnreachable;
        if (temp != null) {
            temp();
        }
    }

    private void OnFullSpeedChanged() {
        InitializeTargetValues();
        AssessFrequencyOfCourseProgressChecks();
    }

    private void OnGameSpeedChanged() {
        _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
        AssessFrequencyOfCourseProgressChecks();
        InitializeTargetValues();
    }

    /// <summary>
    /// Initializes the values that depend on the target and speed.
    /// </summary>
    private void InitializeTargetValues() {
        // IMPROVE proportional to actual Speed? Sync with TargetInfo(StationaryLocation) constructor
        _waypointCloseEnoughDistance = _data.FullSpeed; // no need for gameSpeedMultiplier as check frequency accounts for it
        _waypointCloseEnoughDistanceSqrd = _waypointCloseEnoughDistance * _waypointCloseEnoughDistance;

        float speedFactor = _data.FullSpeed * _gameSpeedMultiplier * 3F;
        __separationTestToleranceDistanceSqrd = speedFactor * speedFactor;   // FIXME needs work - courseUpdatePeriod???
        //D.Log("{0} SeparationToleranceSqrd = {1}, FullSpeed = {2}.", Data.FullName, __separationTestToleranceDistanceSqrd, Data.FullSpeed);

        _courseCorrectionCheckCountSetting = Mathf.RoundToInt(1000 / (speedFactor * 5));  // higher speeds mean a shorter period between course checks, aka more frequent checks
        _courseCorrectionCheckDistanceThresholdSqrd = speedFactor * speedFactor;   // higher speeds mean course checks become continuous further away

        D.Assert(_targetInfo != null, "{0}.Helm targetInfo is null, implying no course has yet been plotted.".Inject(_ship.FullName));
        if (_targetInfo.Target != null && !_targetInfo.Target.IsMovable) {  // target can be null
            // the target doesn't move so course checks are much less important
            _courseCorrectionCheckCountSetting *= 5;
            _courseCorrectionCheckDistanceThresholdSqrd /= 5F;
        }
        _courseCorrectionCheckDistanceThresholdSqrd = Mathf.Max(_courseCorrectionCheckDistanceThresholdSqrd, _targetInfo.CloseEnoughDistanceSqrd * 2);
        //D.Log("{0}: CourseCheckPeriod = {1}, CourseCheckDistanceThreshold = {2}.", Data.FullName, _courseHeadingCheckPeriod, Mathf.Sqrt(_courseHeadingCheckDistanceThresholdSqrd));

        // heading change coroutine could be interrupted immediately after begun leaving isBearingConfirmed false, even while it is still actually true
        // if the first move command was to proceed on the current bearing, then no turn would begin to fix this state, and isBearing would never become true
        IsBearingConfirmed = _data.CurrentHeading.IsSameDirection(_data.RequestedHeading, 1F);
    }

    /// <summary>
    /// Checks whether the pilot can approach the provided location directly.
    /// </summary>
    /// <param name="location">The location to approach.</param>
    /// <returns>
    ///   <c>true</c> if there is nothing obstructing a direct approach.
    /// </returns>
    private bool CheckApproachTo(Vector3 location) {
        Vector3 currentPosition = _data.Position;
        Vector3 vectorToLocation = location - currentPosition;
        float distanceToLocation = vectorToLocation.magnitude;
        float closeEnoughDistance = location.IsSame(_targetInfo.Destination) ? _targetInfo.CloseEnoughDistance : _waypointCloseEnoughDistance;
        if (distanceToLocation < closeEnoughDistance) {
            // already inside close enough distance
            return true;
        }
        Vector3 directionToLocation = vectorToLocation.normalized;
        float rayDistance = distanceToLocation - closeEnoughDistance;
        float clampedRayDistance = Mathf.Clamp(rayDistance, 0.1F, Mathf.Infinity);
        RaycastHit hitInfo;
        if (Physics.Raycast(currentPosition, directionToLocation, out hitInfo, clampedRayDistance, _keepoutOnlyLayerMask.value)) {
            D.Log("{0} encountered obstacle {1} when checking approach to {2}.", _ship.FullName, hitInfo.collider.name, location);
            // there is a keepout zone obstacle in the way 
            return false;
        }
        return true;
    }


    private void InitiateDirectCourseToTarget() {
        D.Log("{0} initiating direct course to target {1}. Distance: {2}.", _ship.FullName, _targetInfo.Target.FullName, Vector3.Magnitude(_targetInfo.Destination - _data.Position));
        D.Assert(!IsAutoPilotEngaged);
        _pilotJob = new Job(EngageDirectCourseToTarget(), true);
    }

    private void InitiateCourseAroundObstacleTo(Vector3 location) {
        D.Log("{0} initiating course to avoid obstacle to {1}. Distance: {2}.", _ship.FullName, location, Vector3.Distance(_data.Position, location));
        DisengageAutoPilot();   // can be called while already engaged
        // even if this is an obstacle that has appeared on the way to another obstacle avoidance waypoint, go around it, then try direct to target
        Vector3 waypointAroundObstacle = GetWaypointAroundObstacleTo(location);
        _pilotJob = new Job(EngageDirectCourseTo(waypointAroundObstacle), true);
        _pilotJob.CreateAndAddChildJob(EngageDirectCourseToTarget());
    }

    /// <summary>
    /// Finds the obstacle obstructing a direct course to the location and develops and
    /// returns a waypoint that will avoid it.
    /// </summary>
    /// <param name="location">The location we are trying to reach that has an obstacle in the way.</param>
    /// <returns>A waypoint location that will avoid the obstacle.</returns>
    private Vector3 GetWaypointAroundObstacleTo(Vector3 location) {
        Vector3 currentPosition = _data.Position;
        Vector3 vectorToLocation = location - currentPosition;
        float distanceToLocation = vectorToLocation.magnitude;
        Vector3 directionToLocation = vectorToLocation.normalized;

        Vector3 waypoint = Vector3.zero;

        Ray ray = new Ray(currentPosition, directionToLocation);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, distanceToLocation, _keepoutOnlyLayerMask.value)) {
            // found a keepout zone, so find the point on the other side of the zone where the ray came out
            string obstacleName = hitInfo.collider.transform.parent.name + "." + hitInfo.collider.name;
            Vector3 rayEntryPoint = hitInfo.point;
            float keepoutRadius = (hitInfo.collider as SphereCollider).radius;
            float maxKeepoutDiameter = TempGameValues.MaxKeepoutRadius * 2F;
            Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + maxKeepoutDiameter);
            if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, maxKeepoutDiameter, _keepoutOnlyLayerMask.value)) {
                Vector3 rayExitPoint = hitInfo.point;
                Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                Vector3 obstacleCenter = hitInfo.collider.transform.position;
                waypoint = obstacleCenter + (halfWayPointInsideKeepoutZone - obstacleCenter).normalized * (keepoutRadius + _targetInfo.CloseEnoughDistance);
                D.Log("{0}'s waypoint to avoid obstacle = {1}.", _ship.FullName, waypoint);
            }
            else {
                D.Error("{0} did not find a ray exit point when casting through {1}.", _ship.FullName, obstacleName);    // hitInfo is null
            }
        }
        else {
            D.Error("{0} did not find an obstacle.", _ship.FullName);
        }
        return waypoint;
    }




    /// <summary>
    /// Engages the ships of the fleet on a direct course to stationary location. No A* course is used.
    /// </summary>
    /// <param name="stationaryLocation">The stationary location.</param>
    /// <returns></returns>
    private IEnumerator EngageDirectCourseTo(Vector3 stationaryLocation) {
        D.Log("{0} beginning prep for direct course to {1}. Distance: {2}.",
            _ship.FullName, stationaryLocation, Vector3.Magnitude(stationaryLocation - _data.Position));
        Vector3 newHeading = (stationaryLocation - _data.Position).normalized;
        if (!newHeading.IsSameDirection(_data.RequestedHeading, 0.1F)) {
            ChangeHeading(newHeading);
        }
        if (_isFleetMove) {
            while (!_ship.Command.IsBearingConfirmed) {
                // wait here until the fleet is ready for departure
                yield return null;
            }
        }
        D.Log("Fleet has matched bearing. {0} powering up. Distance to {1}: {2}.",
            _ship.FullName, stationaryLocation, Vector3.Magnitude(stationaryLocation - _data.Position));

        int courseCorrectionCheckCountdown = _courseCorrectionCheckCountSetting;
        bool isSpeedChecked = false;

        float distanceToDestinationSqrd = Vector3.SqrMagnitude(stationaryLocation - _data.Position);
        float previousDistanceSqrd = distanceToDestinationSqrd;

        while (distanceToDestinationSqrd > _waypointCloseEnoughDistanceSqrd) {
            D.Log("{0} distance to {1} = {2}.", _ship.FullName, stationaryLocation, Vector3.Magnitude(stationaryLocation - _data.Position));
            if (!isSpeedChecked) {    // adjusts speed as a oneshot until we get there
                isSpeedChecked = AdjustSpeedOnHeadingConfirmation();
            }
            Vector3 correctedHeading;
            if (CheckForCourseCorrection(stationaryLocation, distanceToDestinationSqrd, out correctedHeading, ref courseCorrectionCheckCountdown)) {
                D.Log("{0} is making a midcourse correction of {1:0.00} degrees.", _ship.FullName, Vector3.Angle(correctedHeading, _data.RequestedHeading));
                AdjustHeadingAndSpeedForTurn(correctedHeading);
                isSpeedChecked = false;
            }

            if (!CheckApproachTo(stationaryLocation)) {    // TODO make less frequent
                InitiateCourseAroundObstacleTo(stationaryLocation);
            }

            if (CheckSeparation(distanceToDestinationSqrd, ref previousDistanceSqrd)) {
                // we've missed the target or its getting away
                OnDestinationUnreachable();
                yield break;
            }
            distanceToDestinationSqrd = Vector3.SqrMagnitude(stationaryLocation - _data.Position);
            yield return new WaitForSeconds(_courseProgressCheckPeriod);
        }
        D.Log("{0} has arrived at {1}.", _ship.FullName, stationaryLocation);
    }











    ///// <summary>
    ///// Checks whether the distance between this ship and its destination is increasing.
    ///// </summary>
    ///// <param name="distanceToCurrentDestinationSqrd">The current distance to the destination SQRD.</param>
    ///// <param name="previousDistanceSqrd">The previous distance SQRD.</param>
    ///// <returns>true if the separation distance is increasing.</returns>
    //private bool CheckSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
    //    if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + __separationTestToleranceDistanceSqrd) {
    //        D.Warn("{0} separating from {1}. DistanceSqrd = {2}, previousSqrd = {3}, tolerance = {4}.", _ship.FullName,
    //            _targetInfo.Target.FullName, distanceToCurrentDestinationSqrd, previousDistanceSqrd, __separationTestToleranceDistanceSqrd);
    //        return true;
    //    }
    //    if (distanceToCurrentDestinationSqrd < previousDistanceSqrd) {
    //        // while we continue to move closer to the current destination, keep previous distance current
    //        // once we start to move away, we must not update it if we want the tolerance check to catch it
    //        previousDistanceSqrd = distanceToCurrentDestinationSqrd;
    //    }
    //    return false;
    //}

    /// <summary>
    /// Checks whether the distance between this ship and its destination is increasing.
    /// </summary>
    /// <param name="distanceToCurrentDestinationSqrd">The current distance to the destination SQRD.</param>
    /// <param name="previousDistanceSqrd">The previous distance SQRD.</param>
    /// <returns>true if the separation distance is increasing.</returns>
    private bool CheckSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
        if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + __separationTestToleranceDistanceSqrd) {
            D.Warn("{0} is separating from current destination. DistanceSqrd = {1}, previousSqrd = {2}, tolerance = {3}.", _ship.FullName,
                distanceToCurrentDestinationSqrd, previousDistanceSqrd, __separationTestToleranceDistanceSqrd);
            return true;
        }
        if (distanceToCurrentDestinationSqrd < previousDistanceSqrd) {
            // while we continue to move closer to the current destination, keep previous distance current
            // once we start to move away, we must not update it if we want the tolerance check to catch it
            previousDistanceSqrd = distanceToCurrentDestinationSqrd;
        }
        return false;
    }


    private void AssessFrequencyOfCourseProgressChecks() {
        // frequency of course progress checks increases as fullSpeed and gameSpeed increase
        float courseProgressCheckFrequency = 1F + (_data.FullSpeed * _gameSpeedMultiplier);
        _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
        //D.Log("{0} frequency of course progress checks adjusted to {1:0.##}.", Data.FullName, courseProgressCheckFrequency);
    }

    private void Cleanup() {
        Unsubscribe();
        if (_pilotJob != null) {
            _pilotJob.Kill();
        }
        if (_headingJob != null) {
            _headingJob.Kill();
        }
        _engineRoom.Dispose();
    }

    private void Unsubscribe() {
        _subscribers.ForAll<IDisposable>(s => s.Dispose());
        _subscribers.Clear();
        // subscriptions contained completely within this gameobject (both subscriber
        // and subscribee) donot have to be cleaned up as all instances are destroyed
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable
    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

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
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = isDisposing;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
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
//}

