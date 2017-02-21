﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHelm2.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// 
    /// </summary>
    public class ShipHelm2 : INavTaskClient {

        /// <summary>
        /// The allowed deviation in degrees to the requestedHeading that is 'close enough'.
        /// </summary>
        private const float AllowedHeadingDeviation = 0.1F;

        private const string DebugNameFormat = "{0}.{1}";

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        private const float DetourTurnAngleThreshold = 15F;

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        private static readonly Speed[] __ValidExternalChangeSpeeds = {
                                                                    Speed.HardStop,
                                                                    Speed.Stop,
                                                                    Speed.ThrustersOnly,
                                                                    Speed.Docking,
                                                                    Speed.DeadSlow,
                                                                    Speed.Slow,
                                                                    Speed.OneThird,
                                                                    Speed.TwoThirds,
                                                                    Speed.Standard,
                                                                    Speed.Full,
                                                                };

        private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        public event EventHandler apCourseChanged;

        public event EventHandler apTargetReached;

        public event EventHandler apTargetUncatchable;

        public bool ShowDebugLog { get { return _ship.ShowDebugLog; } }

        public string DebugName { get { return DebugNameFormat.Inject(_ship.DebugName, typeof(ShipHelm2).Name); } }

        public bool IsPilotEngaged { get; private set; }


        /// <summary>
        /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
        /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
        /// </summary>
        public bool IsActivelyUnderway {
            get {
                //D.Log(ShowDebugLog, "{0}.IsActivelyUnderway called: Pilot = {1}, Propulsion = {2}, Turning = {3}.",
                //    DebugName, IsPilotEngaged, _engineRoom.IsPropulsionEngaged, IsTurnUnderway);
                return IsPilotEngaged || _engineRoom.IsPropulsionEngaged || IsTurnUnderway;
            }
        }

        /// <summary>
        /// The course this AutoPilot will follow when engaged. 
        /// </summary>
        public IList<IShipNavigable> ApCourse { get; private set; }

        public bool IsTurnUnderway { get { return _chgHeadingJob != null; } }

        /// <summary>
        /// The Speed the ship is currently generating propulsion for.
        /// </summary>
        public Speed CurrentSpeedSetting { get { return _shipData.CurrentSpeedSetting; } }


        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private AutoPilotDestinationProxy ApTargetProxy { get; set; }

        private string ApTargetFullName {
            get { return ApTargetProxy != null ? ApTargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float ApTargetDistance { get { return Vector3.Distance(Position, ApTargetProxy.Position); } }

        public Vector3 Position { get { return _ship.Position; } }

        /// <summary>
        /// The initial speed the autopilot should travel at. 
        /// </summary>
        public Speed ApSpeed { get; set; }

        /// <summary>
        /// Indicates whether this is a coordinated fleet move or a move by the ship on its own to the Target.
        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
        /// moving in formation and moving at speeds the whole fleet can maintain.
        /// </summary>
        private bool _isApFleetwideMove;

        /// <summary>
        /// Indicates whether the current speed of the ship is a fleet-wide value or ship-specific.
        /// Valid only while the Pilot is engaged.
        /// </summary>
        private bool _isApCurrentSpeedFleetwide;
        public bool IsApCurrentSpeedFleetwide {
            get { return _isApCurrentSpeedFleetwide; }
        }

        public float UnitFullSpeedValue { get { return _ship.Command.UnitFullSpeedValue; } }

        public float FullSpeedValue { get { return _shipData.FullSpeedValue; } }

        public Vector3 IntendedHeading { get { return _shipData.IntendedHeading; } }

        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _apActionToExecuteWhenFleetIsAligned;

        /// <summary>
        /// Indicates whether the Pilot is continuously pursuing the target. If <c>true</c> the pilot
        /// will continue to pursue the target even after it dies. Clients are responsible for disengaging the
        /// pilot in circumstances like this. If<c>false</c> the Pilot will report back to the ship when it
        /// arrives at the target.
        /// </summary>
        private bool _isApInPursuit;
        private bool _doesApObstacleCheckPeriodNeedRefresh;
        private GameTimeDuration _apObstacleCheckPeriod;

        ////private Job _apMaintainPositionWhilePursuingJob;
        private Job _apObstacleCheckJob;
        private Job _chgHeadingJob;

        private IList<IDisposable> _subscriptions;
        private GameTime _gameTime;
        private IJobManager _jobMgr;
        private IShip _ship;
        private ShipData _shipData;
        private EngineRoom _engineRoom;
        private Transform _shipTransform;
        //private GameManager _gameMgr;

        private NavTask _navTask;
        private MaintainPositionTask _maintainPositionTask;

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHelm" /> class.
        /// </summary>
        /// <param name="ship">The ship.</param>
        /// <param name="shipRigidbody">The ship rigidbody.</param>
        public ShipHelm2(IShip ship, ShipData shipData, Transform shipTransform, EngineRoom engineRoom) {
            ApCourse = new List<IShipNavigable>();
            //_gameMgr = GameManager.Instance;
            _gameTime = GameTime.Instance;
            _jobMgr = References.JobManager;

            _ship = ship;
            _shipData = shipData;
            _shipTransform = shipTransform;
            _engineRoom = engineRoom;
            Subscribe();
        }

        private void Subscribe() {
            _subscriptions = new List<IDisposable>();
            _subscriptions.Add(_shipData.SubscribeToPropertyChanged<ShipData, float>(d => d.FullSpeedValue, FullSpeedPropChangedHandler));
        }

        /// <summary>
        /// Engages the pilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the pilot should travel at.</param>
        /// <param name="isFleetwideMove">if set to <c>true</c> [is fleetwide move].</param>
        public void EngagePilotToMoveTo(AutoPilotDestinationProxy apTgtProxy, Speed speed, bool isFleetwideMove) {
            Utility.ValidateNotNull(apTgtProxy);
            D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
            ApTargetProxy = apTgtProxy;
            ApSpeed = speed;
            _isApFleetwideMove = isFleetwideMove;
            _isApCurrentSpeedFleetwide = isFleetwideMove;
            _isApInPursuit = false;
            RefreshCourse(CourseRefreshMode.NewCourse);
            EngagePilot();
        }

        /// <summary>
        /// Engages the pilot to pursue the target using the provided proxy. "Pursuit" here
        /// entails continuously adjusting speed and heading to stay within the arrival window
        /// provided by the proxy. There is no 'notification' to the ship as the pursuit never
        /// terminates until the pilot is disengaged by the ship.
        /// </summary>
        /// <param name="apTgtProxy">The proxy for the target this Pilot is being engaged to pursue.</param>
        /// <param name="apSpeed">The initial speed used by the pilot.</param>
        public void EngagePilotToPursue(AutoPilotDestinationProxy apTgtProxy, Speed apSpeed) {
            Utility.ValidateNotNull(apTgtProxy);
            ApTargetProxy = apTgtProxy;
            ApSpeed = apSpeed;
            _isApFleetwideMove = false;
            _isApCurrentSpeedFleetwide = false;
            _isApInPursuit = true;
            RefreshCourse(CourseRefreshMode.NewCourse);
            EngagePilot();
        }

        /// <summary>
        /// Internal method that engages the pilot.
        /// </summary>
        private void EngagePilot() {
            D.Assert(!IsPilotEngaged);
            D.Assert(ApCourse.Count != Constants.Zero, DebugName);
            // Note: A heading job launched by the captain should be overridden when the pilot becomes engaged
            CleanupAnyRemainingJobs();
            //D.Log(ShowDebugLog, "{0} Pilot engaging.", DebugName);
            IsPilotEngaged = true;

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (ApTargetProxy.HasArrived(Position)) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, ApTargetProxy.DebugName);
                HandleTargetReached();
                return;
            }
            if (ShowDebugLog && ApTargetDistance < ApTargetProxy.InnerRadius) {
                D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, ApTargetProxy.DebugName);
            }

            AutoPilotDestinationProxy detour;
            if (TryCheckForObstacleEnrouteTo(ApTargetProxy, out detour)) {
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                InitiateCourseToTargetVia(detour);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }

        #endregion

        #region Course Navigation

        /// <summary>
        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
        /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        private void InitiateDirectCourseToTarget() {
            D.AssertNull(_apObstacleCheckJob);
            D.AssertNull(_apActionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            if (targetBearing.IsSameAs(Vector3.zero)) {
                D.Error("{0} ordered to move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, ApTargetFullName);
            }
            if (_isApFleetwideMove) {
                ChangeHeading_Internal(targetBearing);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", DebugName, _ship.Command.Name, ApTargetFullName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    bool isNavInitiated = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                (_ship.Command as IFleetCmd).WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
                    EngageEnginesAtApSpeed(isFleetSpeed: false);
                    bool isNavInitiated = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                });
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It waits for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
        private void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
            D.AssertNull(_apObstacleCheckJob);
            D.AssertNull(_apActionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (newHeading.IsSameAs(Vector3.zero)) {
                D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
            }
            if (_isApFleetwideMove) {
                ChangeHeading_Internal(newHeading);

                _apActionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
                    _apActionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                (_ship.Command as IFleetCmd).WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                });
            }
        }

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon completion of a detour routing around an obstacle.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        public void ResumeDirectCourseToTarget() {
            CleanupAnyRemainingJobs();   // always called while already engaged
                                         //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
                                         //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (ApTargetProxy.Position - Position).normalized;
            ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
                bool isNavInitiated = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                    HandleTargetReached();
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                }
            });
        }

        /// <summary>
        /// Continues the course to target via the provided obstacleDetour. Called while underway upon encountering an obstacle.
        /// </summary>
        /// <param name="obstacleDetour">The obstacle detour's proxy.</param>
        private void ContinueCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
            CleanupAnyRemainingJobs();   // always called while already engaged
                                         //D.Log(ShowDebugLog, "{0} continuing course to target {1} via obstacle detour {2}. Distance to detour = {3:0.0}.",
                                         //    DebugName, ApTargetFullName, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach obstacle detour {1}.", DebugName, obstacleDetour.DebugName);
                bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                }
            });
        }

        /// <summary>
        /// Initiates navigation to the destination indicated by destProxy, returning <c>true</c> if navigation was initiated,
        /// <c>false</c> if navigation is not needed since already present at destination.
        /// </summary>
        /// <param name="destProxy">The destination proxy.</param>
        /// <param name="hasArrived">Delegate executed when the ship has arrived at the destination.</param>
        /// <returns></returns>
        private bool InitiateNavigationTo(AutoPilotDestinationProxy destProxy, Action hasArrived = null) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            if (!_engineRoom.IsPropulsionEngaged) {
                D.Error("{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}", DebugName, destProxy.DebugName, ApSpeed.GetValueName());
            }
            if (_navTask != null) {
                D.Assert(!_navTask.IsEngaged);
            }

            float distanceToArrival;
            Vector3 directionToArrival;
#pragma warning disable 0219
            bool isArrived = false;
#pragma warning restore 0219
            if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
                // arrived
                if (hasArrived != null) {
                    hasArrived();
                }
                return false;   // already arrived so nav not initiated
            }
            else {

                _navTask = _navTask ?? new NavTask(this, _engineRoom);
                bool isDestinationADetour = destProxy != ApTargetProxy;
                bool isDestFastMover = destProxy.IsFastMover;
                _navTask.IsIncreaseAboveApSpeedAllowed = isDestinationADetour || isDestFastMover;
                _navTask.RunTask(destProxy, hasArrived);
                return true;
            }
        }

        /// <summary>
        /// Calculates and returns the world space offset to the provided detour that when combined with the
        /// detour's position, represents the actual location in world space this ship is trying to reach, 
        /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <returns></returns>
        private Vector3 CalcDetourOffset(StationaryLocation detour) {
            if (_isApFleetwideMove) {
                // make separate detour offsets as there may be a lot of ships encountering this detour
                Quaternion shipCurrentRotation = _shipTransform.rotation;
                Vector3 shipToDetourDirection = (detour.Position - _ship.Position).normalized;
                Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(_ship.CurrentHeading, shipToDetourDirection);
                Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
                Vector3 shipLocalFormationOffset = _ship.FormationStation.LocalOffset;
                Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, shipLocalFormationOffset);
                return detourWorldSpaceOffset;
            }
            return Vector3.zero;
        }

        #endregion

        #region Change Heading

        /// <summary>
        /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
        /// For use when managing the heading of the ship without using the Autopilot.
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="headingConfirmed">Delegate that fires when the ship gets to the new heading.</param>
        public void ChangeHeading(Vector3 newHeading, Action headingConfirmed = null) {
            DisengagePilot(); // kills ChangeHeading job if pilot running
            if (IsTurnUnderway) {
                D.Warn("{0} received sequential ChangeHeading calls from Captain.", DebugName);
            }
            ChangeHeading_Internal(newHeading, headingConfirmed);
        }

        /// <summary>
        /// Changes the direction the ship is headed. 
        /// </summary>
        /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
        /// <param name="headingConfirmed">Delegate that fires when the ship gets to the new heading.</param>
        public void ChangeHeading_Internal(Vector3 newHeading, Action headingConfirmed = null) {
            newHeading.ValidateNormalized();
            //D.Log(ShowDebugLog, "{0} received ChangeHeading to (local){1}.", DebugName, _shipTransform.InverseTransformDirection(newHeading));

            // Warning: Don't test for same direction here. Instead, if same direction, let the coroutine respond one frame
            // later. Reasoning: If previous Job was just killed, next frame it will assert that the autoPilot isn't engaged. 
            // However, if same direction is determined here, then onHeadingConfirmed will be
            // executed before that assert test occurs. The execution of onHeadingConfirmed() could initiate a new autopilot order
            // in which case the assert would fail the next frame. By allowing the coroutine to respond, that response occurs one frame later,
            // allowing the assert to successfully pass before the execution of onHeadingConfirmed can initiate a new autopilot order.

            if (IsTurnUnderway) {
                // 5.8.16 allowing heading changes to kill existing heading jobs so course corrections don't get skipped if job running
                //D.Log(ShowDebugLog, "{0} is killing existing change heading job and starting another. Frame: {1}.", DebugName, Time.frameCount);
                KillChgHeadingJob();
            }

            D.AssertNull(_chgHeadingJob, DebugName);

            _shipData.IntendedHeading = newHeading;
            _engineRoom.HandleTurnBeginning();

            string jobName = "{0}.ChgHeadingJob".Inject(DebugName);
            _chgHeadingJob = _jobMgr.StartGameplayJob(ChangeHeading(newHeading), jobName, isPausable: true, jobCompleted: (jobWasKilled) => {
                if (jobWasKilled) {
                    // 5.8.16 Killed scenarios better understood: 1) External ChangeHeading call while in AutoPilot, 
                    // 2) sequential external ChangeHeading calls, 3) AutoPilot detouring around an obstacle,  
                    // 4) AutoPilot resuming course to Target after detour, 5) AutoPilot course correction, and
                    // 6) 12.9.16 JobManager kill at beginning of scene change.

                    // Thoughts: All Killed scenarios will result in an immediate call to this ChangeHeading_Internal method. Responding now 
                    // (a frame later) with either onHeadingConfirmed or changing _ship.IsHeadingConfirmed is unnecessary and potentially 
                    // wrong. It is unnecessary since the new ChangeHeading_Internal call will set IsHeadingConfirmed correctly and respond 
                    // with onHeadingConfirmed() as soon as the new ChangeHeading Job properly finishes. 
                    // UNCLEAR Thoughts on potentially wrong: Which onHeadingConfirmed delegate would be executed? 1) the previous source of the 
                    // ChangeHeading order which is probably not listening (the autopilot navigation Job has been killed and may be about 
                    // to be replaced by a new one) or 2) the new source that generated the kill? If it goes to the new source, 
                    // that is going to be accomplished anyhow as soon as the ChangeHeading Job launched by the new source determines 
                    // that the heading is confirmed so a response here would be a duplicate. 
                    // 12.7.16 Almost certainly 1) as the delegate creates another complete class to hold all the values that 
                    // need to be executed when fired.

                    // 12.12.16 An AssertNull(_jobRef) here can fail as the reference can refer to a new Job, created 
                    // right after the old one was killed due to the 1 frame delay in execution of jobCompleted(). My attempts at allowing
                    // the AssertNull to occur failed. I believe this is OK as _jobRef is nulled from KillXXXJob() and, if 
                    // the reference is replaced by a new Job, then the old Job is no longer referenced which is the objective. Jobs Kill()ed
                    // centrally by JobManager won't null the reference, but this only occurs during scene transitions.
                }
                else {
                    D.AssertNotNull(_chgHeadingJob, DebugName);
                    _chgHeadingJob = null;
                    //D.Log(ShowDebugLog, "{0}'s turn to {1} complete.  Deviation = {2:0.00} degrees.",
                    //DebugName, _shipData.IntendedHeading, Vector3.Angle(_shipData.CurrentHeading, _shipData.IntendedHeading));
                    _engineRoom.HandleTurnCompleted();
                    if (headingConfirmed != null) {
                        headingConfirmed();
                    }
                }
            });
        }

        /// <summary>
        /// Executes a heading change.
        /// </summary>
        /// <param name="requestedHeading">The requested heading.</param>
        /// <returns></returns>
        private IEnumerator ChangeHeading(Vector3 requestedHeading) {
            D.Assert(!_engineRoom.IsDriftCorrectionUnderway);

            Profiler.BeginSample("Ship ChangeHeading Job Setup", _shipTransform);
            bool isInformedOfDateWarning = false;
            __ResetTurnTimeWarningFields();

            //int startingFrame = Time.frameCount;
            Quaternion startingRotation = _shipTransform.rotation;
            Quaternion intendedHeadingRotation = Quaternion.LookRotation(requestedHeading);
            float desiredTurn = Quaternion.Angle(startingRotation, intendedHeadingRotation);
            D.Log(ShowDebugLog, "{0} initiating turn of {1:0.#} degrees at {2:0.} degrees/hour. AllowedHeadingDeviation = {3:0.##} degrees.",
                DebugName, desiredTurn, _shipData.MaxTurnRate, AllowedHeadingDeviation);
#pragma warning disable 0219
            GameDate currentDate = _gameTime.CurrentDate;
#pragma warning restore 0219

            float deltaTime;
            float deviationInDegrees;
            GameDate warnDate = DebugUtility.CalcWarningDateForRotation(_shipData.MaxTurnRate);
            bool isRqstdHeadingReached = _ship.CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, AllowedHeadingDeviation);
            Profiler.EndSample();

            while (!isRqstdHeadingReached) {
                //D.Log(ShowDebugLog, "{0} continuing another turn step. LastDeviation = {1:0.#} degrees, AllowedDeviation = {2:0.#}.", DebugName, deviationInDegrees, SteeringInaccuracy);

                Profiler.BeginSample("Ship ChangeHeading Job Execution", _shipTransform);
                deltaTime = _gameTime.DeltaTime;
                float allowedTurn = _shipData.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond * deltaTime;
                __allowedTurns.Add(allowedTurn);

                Quaternion currentRotation = _shipTransform.rotation;
                Quaternion inprocessRotation = Quaternion.RotateTowards(currentRotation, intendedHeadingRotation, allowedTurn);
                float actualTurn = Quaternion.Angle(currentRotation, inprocessRotation);
                __actualTurns.Add(actualTurn);

                //Vector3 headingBeforeRotation = _ship.CurrentHeading;
                _shipTransform.rotation = inprocessRotation;
                //D.Log(ShowDebugLog, "{0} BEFORE ROTATION heading: {1}, AFTER ROTATION heading: {2}, rotationApplied: {3}.",
                //    DebugName, headingBeforeRotation.ToPreciseString(), _ship.CurrentHeading.ToPreciseString(), inprocessRotation);

                isRqstdHeadingReached = _ship.CurrentHeading.IsSameDirection(requestedHeading, out deviationInDegrees, AllowedHeadingDeviation);
                if (!isRqstdHeadingReached && (currentDate = _gameTime.CurrentDate) > warnDate) {
                    float resultingTurn = Quaternion.Angle(startingRotation, inprocessRotation);
                    __ReportTurnTimeWarning(warnDate, currentDate, desiredTurn, resultingTurn, __allowedTurns, __actualTurns, ref isInformedOfDateWarning);
                }
                Profiler.EndSample();

                yield return null; // WARNING: must count frames between passes if use yield return WaitForSeconds()
            }
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.##}, ErrorDate = {2}, ActualDate = {3}.",
            //    DebugName, desiredTurn, errorDate, currentDate);
            //D.Log(ShowDebugLog, "{0}: Rotation completed. DegreesRotated = {1:0.#}, FramesReqd = {2}, AvgDegreesPerFrame = {3:0.#}.",
            //    DebugName, desiredTurn, Time.frameCount - startingFrame, desiredTurn / (Time.frameCount - startingFrame));
        }

        #endregion

        #region Change Speed

        /// <summary>
        /// Used by the Pilot to initially engage the engines at ApSpeed.
        /// </summary>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        private void EngageEnginesAtApSpeed(bool isFleetSpeed) {
            D.Assert(IsPilotEngaged);
            //D.Log(ShowDebugLog, "{0} Pilot is engaging engines at speed {1}.", DebugName, ApSpeed.GetValueName());
            ChangeSpeed_Internal(ApSpeed, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeApSpeed() {
            D.Assert(IsPilotEngaged);
            //D.Log(ShowDebugLog, "{0} Pilot is resuming speed {1}.", DebugName, ApSpeed.GetValueName());
            ChangeSpeed_Internal(ApSpeed, isFleetSpeed: false);
        }

        /// <summary>
        /// Primary exposed control that changes the speed of the ship and disengages the pilot.
        /// For use when managing the speed of the ship without relying on  the Autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        public void ChangeSpeed(Speed newSpeed) {
            D.Assert(__ValidExternalChangeSpeeds.Contains(newSpeed), newSpeed.GetValueName());
            //D.Log(ShowDebugLog, "{0} is about to disengage pilot and change speed to {1}.", DebugName, newSpeed.GetValueName());
            DisengagePilot();
            ChangeSpeed_Internal(newSpeed, isFleetSpeed: false);
        }

        /// <summary>
        /// Internal control that changes the speed the ship is currently traveling at. 
        /// This version does not disengage the autopilot.
        /// </summary>
        /// <param name="newSpeed">The new speed.</param>
        /// <param name="moveMode">The move mode.</param>
        public void ChangeSpeed_Internal(Speed newSpeed, bool isFleetSpeed) {
            float newSpeedValue = isFleetSpeed ? newSpeed.GetUnitsPerHour(_ship.Command.UnitFullSpeedValue) : newSpeed.GetUnitsPerHour(_shipData.FullSpeedValue);
            _engineRoom.ChangeSpeed(newSpeed, newSpeedValue);
            if (IsPilotEngaged) {
                _isApCurrentSpeedFleetwide = isFleetSpeed;
            }
        }

        /// <summary>
        /// Refreshes the engine room speed values. This method is called whenever there is a change
        /// in this ship's FullSpeed value or the fleet's FullSpeed value that could change the units/hour value
        /// of the current speed. 
        /// </summary>
        private void RefreshEngineRoomSpeedValues(bool isFleetSpeed) {
            //D.Log(ShowDebugLog, "{0} is refreshing engineRoom speed values.", DebugName);
            ChangeSpeed_Internal(CurrentSpeedSetting, isFleetSpeed);
        }

        #endregion

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            D.AssertNull(_apObstacleCheckJob, DebugName);
            _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
            AutoPilotDestinationProxy detourProxy;
            string jobName = "{0}.ApObstacleCheckJob".Inject(DebugName);
            _apObstacleCheckJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => _apObstacleCheckPeriod), jobName, waitMilestone: () => {

                Profiler.BeginSample("Ship ApObstacleCheckJob Execution", _shipTransform);
                if (TryCheckForObstacleEnrouteTo(destProxy, out detourProxy)) {
                    KillApObstacleCheckJob();
                    RefreshCourse(courseRefreshMode, detourProxy);
                    Profiler.EndSample();
                    ContinueCourseToTargetVia(detourProxy);
                    return;
                }
                if (_doesApObstacleCheckPeriodNeedRefresh) {
                    _apObstacleCheckPeriod = __GenerateObstacleCheckPeriod();
                    _doesApObstacleCheckPeriodNeedRefresh = false;
                }
                Profiler.EndSample();

            });
        }

        private GameTimeDuration __GenerateObstacleCheckPeriod() {
            float relativeObstacleFreq;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
            float defaultHours;
            ValueRange<float> hoursRange;
            switch (_ship.Topography) {
                case Topography.OpenSpace:
                    relativeObstacleFreq = 40F;
                    defaultHours = 20F;
                    hoursRange = new ValueRange<float>(5F, 100F);
                    break;
                case Topography.System:
                    relativeObstacleFreq = 4F;
                    defaultHours = 3F;
                    hoursRange = new ValueRange<float>(1F, 10F);
                    break;
                case Topography.DeepNebula:
                case Topography.Nebula:
                case Topography.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_ship.Topography));
            }
            float speedValue = _engineRoom.IntendedCurrentSpeedValue;
            float hoursBetweenChecks = speedValue > Constants.ZeroF ? relativeObstacleFreq / speedValue : defaultHours;
            hoursBetweenChecks = hoursRange.Clamp(hoursBetweenChecks);
            hoursBetweenChecks = VaryCheckPeriod(hoursBetweenChecks);

            float checksPerHour = 1F / hoursBetweenChecks;
            if (checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > References.FpsReadout.FramesPerSecond) {
                // check frequency is higher than the game engine can run
                D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
                    DebugName, checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, References.FpsReadout.FramesPerSecond);
            }
            return new GameTimeDuration(hoursBetweenChecks);
        }

        /// <summary>
        /// Checks for an obstacle en-route to the provided <c>destProxy</c>. Returns true if one
        /// is found that requires immediate action and provides the detour to avoid it, false otherwise.
        /// </summary>
        /// <param name="destProxy">The destination proxy. May be the AutoPilotTarget or an obstacle detour.</param>
        /// <param name="detourProxy">The resulting obstacle detour proxy.</param>
        /// <returns>
        ///   <c>true</c> if an obstacle was found and a detour generated, false if the way is effectively clear.
        /// </returns>
        private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            Profiler.BeginSample("Ship TryCheckForObstacleEnrouteTo Execution", _ship.transform);
            int iterationCount = Constants.Zero;
            IAvoidableObstacle unusedObstacleFound;
            bool hasDetour = TryCheckForObstacleEnrouteTo(destProxy, out detourProxy, out unusedObstacleFound, ref iterationCount);
            Profiler.EndSample();
            return hasDetour;
        }

        private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy, out IAvoidableObstacle obstacle, ref int iterationCount) {
            __ValidateIterationCount(iterationCount, destProxy, allowedIterations: 10);
            iterationCount++;
            detourProxy = null;
            obstacle = null;
            Vector3 destBearing = (destProxy.Position - Position).normalized;
            float rayLength = destProxy.GetObstacleCheckRayLength(Position);
            Ray ray = new Ray(Position, destBearing);

            bool isDetourGenerated = false;
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
                // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
                // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
                var obstacleZoneGo = hitInfo.collider.gameObject;
                var obstacleZoneHitDistance = hitInfo.distance;
                obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

                if (obstacle == destProxy.Destination) {
                    D.LogBold(ShowDebugLog, "{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.",
                        DebugName, obstacle.DebugName, rayLength, obstacleZoneHitDistance);
                    HandleObstacleFoundIsTarget(obstacle);
                }
                else {
                    D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                        DebugName, obstacle.DebugName, obstacle.Position, destProxy.DebugName, rayLength, obstacleZoneHitDistance);
                    if (TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detourProxy)) {
                        AutoPilotDestinationProxy newDetourProxy;
                        IAvoidableObstacle newObstacle;
                        if (TryCheckForObstacleEnrouteTo(detourProxy, out newDetourProxy, out newObstacle, ref iterationCount)) {
                            if (obstacle == newObstacle) {
                                // 2.7.17 UNCLEAR redundant? IAvoidableObstacle.GetDetour() should fail if can't get to detour, although check uses math rather than a ray
                                D.Error("{0} generated detour {1} that does not get around obstacle {2}.", DebugName, newDetourProxy.DebugName, obstacle.DebugName);
                            }
                            else {
                                D.Log(ShowDebugLog, "{0} found another obstacle {1} on the way to detour {2} around obstacle {3}.", DebugName, newObstacle.DebugName, detourProxy.DebugName, obstacle.DebugName);
                            }
                            detourProxy = newDetourProxy;
                            obstacle = newObstacle; // UNCLEAR whether useful. 2.7.17 Only use is to compare whether obstacle is the same
                        }
                        isDetourGenerated = true;
                    }
                }
            }
            return isDetourGenerated;
        }

        /// <summary>
        /// Tries to generate a detour around the provided obstacle. Returns <c>true</c> if a detour
        /// was generated, <c>false</c> otherwise. 
        /// <remarks>A detour can always be generated around an obstacle. However, this algorithm considers other factors
        /// before initiating a heading change to redirect to a detour. E.g. moving obstacles that are far away 
        /// and/or require only a small change in heading may not necessitate a diversion to a detour yet.
        /// </remarks>
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        /// <param name="zoneHitInfo">The zone hit information.</param>
        /// <param name="detourProxy">The resulting detour including any reqd offset for the ship when traveling as a fleet.</param>
        /// <returns></returns>
        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out AutoPilotDestinationProxy detourProxy) {
            detourProxy = GenerateDetourAroundObstacle(obstacle, zoneHitInfo);
            if (MyMath.DoesLineSegmentIntersectSphere(Position, detourProxy.Position, obstacle.Position, obstacle.__ObstacleZoneRadius)) {
                // 1.26.17 This can marginally fail when traveling as a fleet when the ship's FleetFormationStation is at the closest edge of the
                // formation to the obstacle. As the proxy incorporates this station offset into its "Position" to keep ships from bunching
                // up when detouring as a fleet, the resulting detour destination can be very close to the edge of the obstacle's Zone.
                // If/when this does occur, I expect the offset to be large.
                D.Warn("{0} generated detour {1} that {2} can't get too because {0} is in the way! Offset = {3:0.00}.", obstacle.DebugName, detourProxy.DebugName, DebugName, detourProxy.__DestinationOffset);
            }

            bool useDetour = true;
            Vector3 detourBearing = (detourProxy.Position - Position).normalized;
            float reqdTurnAngleToDetour = Vector3.Angle(_ship.CurrentHeading, detourBearing);
            if (obstacle.IsMobile) {
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    useDetour = false;
                    // angle is still shallow but short remaining distance might require use of a detour
                    float maxDistanceTraveledBeforeNextObstacleCheck = _engineRoom.IntendedCurrentSpeedValue * _apObstacleCheckPeriod.TotalInHours;
                    float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;   // HACK
                    float distanceToObstacleZone = zoneHitInfo.distance;
                    if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
                        useDetour = true;
                    }
                }
            }
            if (useDetour) {
                D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2} in Frame {3}. Reqd Turn = {4:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, Time.frameCount, reqdTurnAngleToDetour);
            }
            else {
                D.Log(ShowDebugLog, "{0} has declined to use detour {1} to get by mobile obstacle {2}. Reqd Turn = {3:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
            }
            return useDetour;
        }

        /// <summary>
        /// Generates a detour around the provided obstacle. Includes any reqd offset for the
        /// ship when traveling as a fleet.
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <returns></returns>
        private AutoPilotDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo) {
            float reqdClearanceRadius = _isApFleetwideMove ? _ship.Command.UnitMaxFormationRadius : _ship.CollisionDetectionZoneRadius;
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, reqdClearanceRadius);
            StationaryLocation detour = new StationaryLocation(detourPosition);
            Vector3 detourOffset = CalcDetourOffset(detour);
            float tgtStandoffDistance = _ship.CollisionDetectionZoneRadius;
            return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, Position);
        }

        private AutoPilotDestinationProxy __initialDestination;
        private IList<AutoPilotDestinationProxy> __destinationRecord;

        private void __ValidateIterationCount(int iterationCount, AutoPilotDestinationProxy destProxy, int allowedIterations) {
            if (iterationCount == Constants.Zero) {
                __initialDestination = destProxy;
            }
            if (iterationCount > Constants.Zero) {
                if (iterationCount == Constants.One) {
                    __destinationRecord = __destinationRecord ?? new List<AutoPilotDestinationProxy>(allowedIterations + 1);
                    __destinationRecord.Clear();
                    __destinationRecord.Add(__initialDestination);
                }
                __destinationRecord.Add(destProxy);
                D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
                    .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
            }
        }

        #endregion

        #region Pursuit

        /// <summary>
        /// Launches a Job to monitor whether the ship needs to move to stay with the target.
        /// </summary>
        private void MaintainPositionWhilePursuing() {
            ChangeSpeed_Internal(Speed.Stop, isFleetSpeed: false);
            //D.Log(ShowDebugLog, "{0} is launching ApMaintainPositionWhilePursuingJob of {1}.", DebugName, ApTargetFullName);
            if (_maintainPositionTask != null) {
                D.Assert(!_maintainPositionTask.IsEngaged);
            }

            _maintainPositionTask = _maintainPositionTask ?? new MaintainPositionTask(this);
            _maintainPositionTask.RunTask(ApTargetProxy);
        }

        #endregion

        #region Event and Property Change Handlers

        private void FullSpeedPropChangedHandler() {
            HandleFullSpeedValueChanged();
        }

        private void OnApCourseChanged() {
            if (apCourseChanged != null) {
                apCourseChanged(this, EventArgs.Empty);
            }
        }

        private void OnApTargetReached() {
            if (apTargetReached != null) {
                apTargetReached(this, EventArgs.Empty);
            }
        }

        private void OnApTargetUncatchable() {
            if (apTargetUncatchable != null) {
                apTargetUncatchable(this, EventArgs.Empty);
            }
        }

        // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
        // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

        #endregion

        private void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
            if (_ship.IsHQ) {
                // should never happen as HQ approach is always direct            
                D.Warn("HQ {0} encountered obstacle {1} which is target.", DebugName, obstacle.DebugName);
            }
            ApTargetProxy.ResetOffset();   // go directly to target

            if (_navTask != null && _navTask.IsEngaged) {
                D.AssertNotNull(_apObstacleCheckJob);
                ResumeDirectCourseToTarget();
            }
            // if no _apNavJob, HandleObstacleFoundIsTarget() call originated from EngagePilot which will InitiateDirectCourseToTarget
        }

        /// <summary>
        /// Handles the death of the ship in both the Helm and EngineRoom.
        /// Should be called from Dead_EnterState, not PrepareForDeathNotification().
        /// </summary>
        public void HandleDeath() {
            D.Assert(!IsPilotEngaged);  // should already be disengaged by Moving_ExitState if needed if in Dead_EnterState
            CleanupAnyRemainingJobs();  // heading job from Captain could be running
        }

        /// <summary>
        /// Called when the ship 'arrives' at the Target.
        /// </summary>
        internal void HandleTargetReached() {
            D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, ApTargetFullName, ApTargetProxy.Position, ApTargetDistance);
            RefreshCourse(CourseRefreshMode.ClearCourse);

            if (_isApInPursuit) {
                MaintainPositionWhilePursuing();
            }
            else {
                OnApTargetReached();
            }
        }

        /// <summary>
        /// Handles the situation where the Ship determines that the ApTarget can't be caught.
        /// <remarks>TODO: Will need for 'can't catch' or out of sensor range when attacking a ship.</remarks>
        /// </summary>
        private void HandleTargetUncatchable() {
            RefreshCourse(CourseRefreshMode.ClearCourse);
            OnApTargetUncatchable();
        }

        public void HandleFleetFullSpeedValueChanged() {
            if (IsPilotEngaged) {
                if (_isApCurrentSpeedFleetwide) {
                    // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(isFleetSpeed: true);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    if (_navTask != null && _navTask.IsEngaged) {
                        _navTask.DoesApProgressCheckPeriodNeedRefresh = true;
                    }
                    _doesApObstacleCheckPeriodNeedRefresh = true;
                }
            }
        }

        private void HandleFullSpeedValueChanged() {
            if (IsPilotEngaged) {
                if (!_isApCurrentSpeedFleetwide) {
                    // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                    RefreshEngineRoomSpeedValues(isFleetSpeed: false);
                    // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                    if (_navTask != null && _navTask.IsEngaged) {
                        _navTask.DoesApProgressCheckPeriodNeedRefresh = true;
                    }
                    _doesApObstacleCheckPeriodNeedRefresh = true;
                }
            }
            else if (_engineRoom.IsPropulsionEngaged) {
                // Propulsion is engaged and not by AutoPilot so must be external SpeedChange from Captain, value change will matter
                RefreshEngineRoomSpeedValues(isFleetSpeed: false);
            }
        }

        public void HandleCourseChanged() {
            OnApCourseChanged();
        }

        /// <summary>
        /// Disengages the pilot but does not change its heading or residual speed.
        /// <remarks>Externally calling ChangeSpeed() or ChangeHeading() will also disengage the pilot
        /// if needed and make a one time change to the ship's speed and/or heading.</remarks>
        /// </summary>
        public void DisengagePilot() {
            if (IsPilotEngaged) {
                //D.Log(ShowDebugLog, "{0} Pilot disengaging.", DebugName);
                IsPilotEngaged = false;
                CleanupAnyRemainingJobs();
                RefreshCourse(CourseRefreshMode.ClearCourse);
                ApSpeed = Speed.None;
                ApTargetProxy = null;
                _isApFleetwideMove = false;
                _isApCurrentSpeedFleetwide = false;
                _doesApObstacleCheckPeriodNeedRefresh = false;
                _apObstacleCheckPeriod = default(GameTimeDuration);
                _isApInPursuit = false;
            }
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="wayPtProxy">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy wayPtProxy = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), AutoPilotCourse.Count);
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.AssertNull(wayPtProxy);
                    ApCourse.Clear();
                    ApCourse.Add(_ship as IShipNavigable);
                    IShipNavigable courseTgt;
                    if (ApTargetProxy.IsMobile) {
                        courseTgt = new MobileLocation(new Reference<Vector3>(() => ApTargetProxy.Position));
                    }
                    else {
                        courseTgt = new StationaryLocation(ApTargetProxy.Position);
                    }
                    ApCourse.Add(courseTgt);  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.AssertEqual(3, ApCourse.Count);
                    ApCourse.RemoveAt(ApCourse.Count - 2);          // changes Course.Count
                    ApCourse.Insert(ApCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.AssertEqual(3, ApCourse.Count);
                    bool isRemoved = ApCourse.Remove(new StationaryLocation(wayPtProxy.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.AssertNull(wayPtProxy);
                    ApCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }

        /// <summary>
        /// Varies the check period by plus or minus 10% to spread out recurring event firing.
        /// </summary>
        /// <param name="hoursPerCheckPeriod">The hours per check period.</param>
        /// <returns></returns>
        private float VaryCheckPeriod(float hoursPerCheckPeriod) {
            return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
        }

        private void ResetNavTask() {
            if (_navTask != null) {
                _navTask.ResetForReuse();
            }
        }

        private void ResetMaintainPursuitTask() {
            if (_maintainPositionTask != null) {
                _maintainPositionTask.ResetForReuse();
            }
        }

        private void KillApObstacleCheckJob() {
            if (_apObstacleCheckJob != null) {
                _apObstacleCheckJob.Kill();
                _apObstacleCheckJob = null;
            }
        }

        private void KillChgHeadingJob() {
            if (_chgHeadingJob != null) {
                //D.Log(ShowDebugLog, "{0}.ChgHeadingJob is about to be killed and nulled in Frame {1}. ChgHeadingJob.IsRunning = {2}.", DebugName, Time.frameCount, ChgHeadingJob.IsRunning);
                _chgHeadingJob.Kill();
                _chgHeadingJob = null;
            }
        }

        ////private void KillApMaintainPositionWhilePursingJob() {
        ////    if (_apMaintainPositionWhilePursuingJob != null) {
        ////        _apMaintainPositionWhilePursuingJob.Kill();
        ////        _apMaintainPositionWhilePursuingJob = null;
        ////    }
        ////}

        #region Cleanup

        private void CleanupAnyRemainingJobs() {
            ResetNavTask();

            KillApObstacleCheckJob();
            KillChgHeadingJob();
            if (_apActionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_apActionToExecuteWhenFleetIsAligned, _ship);
                _apActionToExecuteWhenFleetIsAligned = null;
            }
            ////KillApMaintainPositionWhilePursingJob();
            ResetMaintainPursuitTask();
        }

        private void Cleanup() {
            Unsubscribe();
            // 12.8.16 Job Disposal centralized in JobManager
            ResetNavTask();

            KillChgHeadingJob();
            KillApObstacleCheckJob();
            ////KillApMaintainPositionWhilePursingJob();
            ResetMaintainPursuitTask();
        }

        private void Unsubscribe() {
            _subscriptions.ForAll(s => s.Dispose());
            _subscriptions.Clear();
        }

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug Turn Error Reporting

        private const string __TurnTimeLineFormat = "Allowed: {0:0.00}, Actual: {1:0.00}";

        private IList<float> __allowedTurns = new List<float>();
        private IList<float> __actualTurns = new List<float>();
        private IList<string> __allowedAndActualTurnSteps;
        private GameDate __turnTimeErrorDate;

        private void __ReportTurnTimeWarning(GameDate warnDate, GameDate currentDate, float desiredTurn, float resultingTurn, IList<float> allowedTurns, IList<float> actualTurns, ref bool isInformedOfDateWarning) {
            if (!isInformedOfDateWarning) {
                D.Log("{0}.ChangeHeading of {1:0.##} degrees. CurrentDate {2} > WarnDate {3}. Turn accomplished: {4:0.##} degrees.",
                    DebugName, desiredTurn, currentDate, warnDate, resultingTurn);
                isInformedOfDateWarning = true;
            }
            if (__turnTimeErrorDate == default(GameDate)) {
                __turnTimeErrorDate = new GameDate(warnDate, GameTimeDuration.OneDay);
            }
            if (currentDate > __turnTimeErrorDate) {
                D.Error("{0}.ChangeHeading timed out.", DebugName);
            }

            if (ShowDebugLog) {
                if (__allowedAndActualTurnSteps == null) {
                    __allowedAndActualTurnSteps = new List<string>();
                }
                __allowedAndActualTurnSteps.Clear();
                for (int i = 0; i < allowedTurns.Count; i++) {
                    string line = __TurnTimeLineFormat.Inject(allowedTurns[i], actualTurns[i]);
                    __allowedAndActualTurnSteps.Add(line);
                }
                D.Log("Allowed vs Actual TurnSteps:\n {0}", __allowedAndActualTurnSteps.Concatenate());
            }
        }

        private void __ResetTurnTimeWarningFields() {
            __allowedTurns.Clear();
            __actualTurns.Clear();
            __turnTimeErrorDate = default(GameDate);
        }

        #endregion

        #region Vector3 ExecuteHeadingChange Archive

        //private IEnumerator ExecuteHeadingChange(float allowedTime) {
        //    //D.Log("{0} initiating turn to heading {1} at {2:0.} degrees/hour.", DebugName, _ship.Data.RequestedHeading, _ship.Data.MaxTurnRate);
        //    float cumTime = Constants.ZeroF;
        //    while (!_ship.IsHeadingConfirmed) {
        //        float maxTurnRateInRadiansPerSecond = Mathf.Deg2Rad * _ship.Data.MaxTurnRate * _gameTime.GameSpeedAdjustedHoursPerSecond;   //GameTime.HoursPerSecond;
        //        float allowedTurn = maxTurnRateInRadiansPerSecond * _gameTime.DeltaTimeOrPaused;
        //        Vector3 newHeading = Vector3.RotateTowards(_ship.Data.CurrentHeading, _ship.Data.RequestedHeading, allowedTurn, maxMagnitudeDelta: 1F);
        //        // maxMagnitudeDelta > 0F appears to be important. Otherwise RotateTowards can stop rotating when it gets very close
        //        _ship.transform.rotation = Quaternion.LookRotation(newHeading); // UNCLEAR turn kinematic on while rotating?
        //                                                                        //D.Log("{0} actual heading after turn step: {1}.", DebugName, _ship.Data.CurrentHeading);
        //        cumTime += _gameTime.DeltaTimeOrPaused;
        //        D.Assert(cumTime < allowedTime, "{0}: CumTime {1:0.##} > AllowedTime {2:0.##}.".Inject(Name, cumTime, allowedTime));
        //        yield return null; // WARNING: have to count frames between passes if use yield return WaitForSeconds()
        //    }
        //    //D.Log("{0} completed HeadingChange Job. Duration = {1:0.##} GameTimeSecs.", DebugName, cumTime);
        //}

        #endregion

        #region SeparationDistance Archive

        //private float __separationTestToleranceDistance;

        /// <summary>
        /// Checks whether the distance between this ship and its destination is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestination">The distance to current destination.</param>
        /// <param name="previousDistance">The previous distance.</param>
        /// <returns>
        /// true if the separation distance is increasing.
        /// </returns>
        //private bool CheckSeparation(float distanceToCurrentDestination, ref float previousDistance) {
        //    if (distanceToCurrentDestination > previousDistance + __separationTestToleranceDistance) {
        //        D.Warn("{0} is separating from current destination. Distance = {1:0.00}, previous = {2:0.00}, tolerance = {3:0.00}.",
        //            _ship.DebugName, distanceToCurrentDestination, previousDistance, __separationTestToleranceDistance);
        //        return true;
        //    }
        //    if (distanceToCurrentDestination < previousDistance) {
        //        // while we continue to move closer to the current destination, keep previous distance current
        //        // once we start to move away, we must not update it if we want the tolerance check to catch it
        //        previousDistance = distanceToCurrentDestination;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Returns the max separation distance the ship and a target moon could create between progress checks. 
        /// This is determined by calculating the max distance the ship could cover moving away from the moon
        /// during a progress check period and adding the max distance a moon could cover moving away from the ship
        /// during a progress check period. A moon is used because it has the maximum potential speed, aka it is in the 
        /// outer orbit slot of a planet which itself is in the outer orbit slot of a system.
        /// This value is very conservative as the ship would only be traveling directly away from the moon at the beginning of a UTurn.
        /// By the time it progressed through 90 degrees of the UTurn, theoretically it would no longer be moving away at all. 
        /// After that it would no longer be increasing its separation from the moon. Of course, most of the time, 
        /// it would need to make a turn of less than 180 degrees, but this is the max. 
        /// IMPROVE use 90 degrees rather than 180 degrees per the argument above?
        /// </summary>
        /// <returns></returns>
        //private float CalcSeparationTestTolerance() {
        //    //var hrsReqdToExecuteUTurn = 180F / _ship.Data.MaxTurnRate;
        //    // HoursPerSecond and GameSpeedMultiplier below cancel each other out
        //    //var secsReqdToExecuteUTurn = hrsReqdToExecuteUTurn / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var speedInUnitsPerSec = _autoPilotSpeedInUnitsPerHour / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var maxDistanceCoveredByShipPerSecond = speedInUnitsPerSec;
        //    //var maxDistanceCoveredExecutingUTurn = secsReqdToExecuteUTurn * speedInUnitsPerSec;
        //    //var maxDistanceCoveredByShipExecutingUTurn = hrsReqdToExecuteUTurn * _autoPilotSpeedInUnitsPerHour;
        //    //var maxUTurnDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipExecutingUTurn * _courseProgressCheckPeriod;
        //    var maxDistanceCoveredByShipPerProgressCheck = maxDistanceCoveredByShipPerSecond * _courseProgressCheckPeriod;
        //    var maxDistanceCoveredByMoonPerSecond = APlanetoidItem.MaxOrbitalSpeed / (GameTime.HoursPerSecond * _gameSpeedMultiplier);
        //    var maxDistanceCoveredByMoonPerProgressCheck = maxDistanceCoveredByMoonPerSecond * _courseProgressCheckPeriod;

        //    var maxSeparationDistanceCoveredPerProgressCheck = maxDistanceCoveredByShipPerProgressCheck + maxDistanceCoveredByMoonPerProgressCheck;
        //    //D.Warn("UTurnHrs: {0}, MaxUTurnDistance: {1}, {2} perProgressCheck, MaxMoonDistance: {3} perProgressCheck.",
        //    //    hrsReqdToExecuteUTurn, maxDistanceCoveredByShipExecutingUTurn, maxUTurnDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerProgressCheck);
        //    //D.Log("ShipMaxDistancePerSecond: {0}, ShipMaxDistancePerProgressCheck: {1}, MoonMaxDistancePerSecond: {2}, MoonMaxDistancePerProgressCheck: {3}.",
        //    //    maxDistanceCoveredByShipPerSecond, maxDistanceCoveredByShipPerProgressCheck, maxDistanceCoveredByMoonPerSecond, maxDistanceCoveredByMoonPerProgressCheck);
        //    return maxSeparationDistanceCoveredPerProgressCheck;
        //}

        #endregion

        #region Debug Slowing Speed Progression Reporting Archive

        //        // Reports how fast speed bleeds off when Slow, Stop, etc are used 

        //        private static Speed[] __constantValueSpeeds = new Speed[] {    Speed.Stop,
        //                                                                        Speed.Docking,
        //                                                                        Speed.StationaryOrbit,
        //                                                                        Speed.MovingOrbit,
        //                                                                        Speed.Slow
        //                                                                    };

        //        private Job __speedProgressionReportingJob;
        //        private Vector3 __positionWhenReportingBegun;

        //        private void __TryReportSlowingSpeedProgression(Speed newSpeed) {
        //            //D.Log(ShowDebugLog, "{0}.TryReportSlowingSpeedProgression({1}) called.", DebugName, newSpeed.GetValueName());
        //            if (__constantValueSpeeds.Contains(newSpeed)) {
        //                __ReportSlowingSpeedProgression(newSpeed);
        //            }
        //            else {
        //                __TryKillSpeedProgressionReportingJob();
        //            }
        //        }

        //        private void __ReportSlowingSpeedProgression(Speed constantValueSpeed) {
        //            D.Assert(!_gameMgr.IsPaused, "Not allowed to create a Job while paused.");
        //            D.Assert(__constantValueSpeeds.Contains(constantValueSpeed), "{0} speed {1} is not a constant value.", _ship.DebugName, constantValueSpeed.GetValueName());
        //            if (__TryKillSpeedProgressionReportingJob()) {
        //                __ReportDistanceTraveled();
        //            }
        //            if (constantValueSpeed == Speed.Stop && ActualSpeedValue == Constants.ZeroF) {
        //                return; // don't bother reporting if not moving and Speed setting is Stop
        //            }
        //            __positionWhenReportingBegun = Position;
        //            __speedProgressionReportingJob = new Job(__ContinuouslyReportSlowingSpeedProgression(constantValueSpeed), toStart: true);
        //        }

        //        private IEnumerator __ContinuouslyReportSlowingSpeedProgression(Speed constantSpeed) {
        //#pragma warning disable 0219    // OPTIMIZE
        //            string desiredSpeedText = "{0}'s Speed setting = {1}({2:0.###})".Inject(_ship.DebugName, constantSpeed.GetValueName(), constantSpeed.GetUnitsPerHour(ShipMoveMode.None, null, null));
        //            float currentSpeed;
        //#pragma warning restore 0219
        //            int fixedUpdateCount = 0;
        //            while ((currentSpeed = ActualSpeedValue) > Constants.ZeroF) {
        //                //D.Log(ShowDebugLog, desiredSpeedText + " ActualSpeed = {0:0.###}, FixedUpdateCount = {1}.", currentSpeed, fixedUpdateCount);
        //                fixedUpdateCount++;
        //                yield return new WaitForFixedUpdate();
        //            }
        //            __ReportDistanceTraveled();
        //        }

        //        private bool __TryKillSpeedProgressionReportingJob() {
        //            if (__speedProgressionReportingJob != null && __speedProgressionReportingJob.IsRunning) {
        //                __speedProgressionReportingJob.Kill();
        //                return true;
        //            }
        //            return false;
        //        }

        //        private void __ReportDistanceTraveled() {
        //            Vector3 distanceTraveledVector = _ship.transform.InverseTransformDirection(Position - __positionWhenReportingBegun);
        //            D.Log(ShowDebugLog, "{0} changed local position by {1} while reporting slowing speed.", _ship.DebugName, distanceTraveledVector);
        //        }

        #endregion


        #region IDisposable

        private bool _alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {

            Dispose(true);

            // This object is being cleaned up by you explicitly calling Dispose() so take this object off
            // the finalization queue and prevent finalization code from 'disposing' a second time
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="isExplicitlyDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isExplicitlyDisposing) {
            if (_alreadyDisposed) { // Allows Dispose(isExplicitlyDisposing) to mistakenly be called more than once
                D.Warn("{0} has already been disposed.", GetType().Name);
                return; //throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
            }

            if (isExplicitlyDisposing) {
                // Dispose of managed resources here as you have called Dispose() explicitly
                Cleanup();
            }

            // Dispose of unmanaged resources here as either 1) you have called Dispose() explicitly so
            // may as well clean up both managed and unmanaged at the same time, or 2) the Finalizer has
            // called Dispose(false) to cleanup unmanaged resources

            _alreadyDisposed = true;
        }

        #endregion

    }
}

