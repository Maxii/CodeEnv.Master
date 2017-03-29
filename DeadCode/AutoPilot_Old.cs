// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoPilot.cs
// AutoPilot that navigates a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// AutoPilot that navigates a ship.
    /// </summary>
    [Obsolete]
    public class AutoPilot_Old {

        private const string DebugNameFormat = "{0}.{1}";

        private static readonly Speed[] InvalidApSpeeds = {
                                                            Speed.None,
                                                            Speed.HardStop,
                                                            Speed.Stop
                                                        };

        internal bool IsEngaged { get; private set; }

        internal string DebugName { get { return DebugNameFormat.Inject(_helm.DebugName, GetType().Name); } }

        internal bool ShowDebugLog { get { return _helm.ShowDebugLog; } }

        /// <summary>
        /// The speed at which the autopilot has been instructed to travel.
        /// <remark>This value does not change while an AutoPilot is engaged.</remark>
        /// </summary>
        internal Speed ApSpeedSetting { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if this Cmd is close enough to support an attack on Target with its SR sensors, <c>false</c> otherwise.
        /// </summary>
        /// <returns></returns>
        internal bool IsCmdWithinRangeToSupportAttackOnTarget {
            get {
                return Vector3.SqrMagnitude(TargetProxy.Position - _ship.Command.Position) < _ship.Command.SRSensorRangeDistance * _ship.Command.SRSensorRangeDistance;
                //bool isCmdWithinSupportRangeOfTgt = Vector3.SqrMagnitude(TargetProxy.Position - _ship.Command.Position) < TempGameValues.__MaxShipAttackRangeFromCmdSqrd;
                //return isCmdWithinSupportRangeOfTgt;
            }
        }

        internal float ReqdObstacleClearanceDistance {
            get { return IsFleetwideMove ? _ship.Command.UnitMaxFormationRadius : _ship.CollisionDetectionZoneRadius; }
        }

        internal float ShipCollisionDetectionZoneRadius { get { return _ship.CollisionDetectionZoneRadius; } }

        /// <summary>
        /// Indicates whether this is a coordinated fleet move or a move by the ship on its own to the Target.
        /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
        /// moving in formation and moving at speeds the whole fleet can maintain.
        /// </summary>
        internal bool IsFleetwideMove { get { return _directive == ApDirective.MoveToAsFleet; } }

        internal Vector3 Position { get { return _ship.Position; } }

        private bool _doesMoveTaskProgressCheckPeriodNeedRefresh;
        internal bool DoesMoveTaskProgressCheckPeriodNeedRefresh {
            get { return _doesMoveTaskProgressCheckPeriodNeedRefresh; }
            set {
                D.Assert(IsEngaged);
                if (_moveTask.IsEngaged) {
                    // period only needs refresh if already engaged
                    _doesMoveTaskProgressCheckPeriodNeedRefresh = value;
                }
            }
        }

        internal bool DoesObstacleCheckTaskPeriodNeedRefresh {
            set {
                D.Assert(IsEngaged);
                if (_obstacleCheckTask.IsEngaged) {
                    // period only needs refresh if already engaged
                    _obstacleCheckTask.DoesObstacleCheckPeriodNeedRefresh = value;
                }
            }
        }

        /// <summary>
        /// Indicates whether the current speed the AutoPilot is operating at is a fleet-wide value or ship-specific.
        /// Valid only while the Pilot is engaged.
        /// </summary>
        internal bool IsCurrentSpeedFleetwide { get; private set; }

        /// <summary>
        /// Indicates the AutoPilot is instructed to attack using ApDirective Bombard or Strafe.
        /// </summary>
        internal bool IsAttacking { get { return _directive == ApDirective.Bombard || _directive == ApDirective.Strafe; } }

        /// <summary>
        /// Indicates the AutoPilot is instructed to attack using ApDirective Strafe.
        /// </summary>
        internal bool IsStrafeAttacking { get { return _directive == ApDirective.Strafe; } }

        /// <summary>
        /// The current speed setting of the ship.
        /// <remarks>This value can be &lt;, == to or &gt; the ApSpeedSetting as the AutoPilot
        /// has the ability to vary the actual speed of the ship. Slowing typically occurs when 
        /// approaching a destination. Increasing typically occurs when coming out of a detour.</remarks>
        /// </summary>
        internal Speed CurrentSpeedSetting { get { return _helm.CurrentSpeedSetting; } }

        internal Quaternion ShipRotation { get { return _shipTransform.rotation; } }

        internal Vector3 ShipLocalFormationOffset { get { return _ship.FormationStation.LocalOffset; } }

        internal bool IsIncreaseAboveApSpeedSettingAllowed { get; private set; }

        internal float IntendedCurrentSpeedValue { get { return _engineRoom.IntendedCurrentSpeedValue; } }

        internal Vector3 IntendedHeading { get { return _helm.IntendedHeading; } }

        internal Vector3 CurrentHeading { get { return _ship.CurrentHeading; } }

        internal float __PreviousIntendedCurrentSpeedValue { get { return _engineRoom.__PreviousIntendedCurrentSpeedValue; } }

        internal Topography __Topography { get { return _ship.Topography; } }

        /// <summary>
        /// The current target (proxy) this Pilot is engaged to reach.
        /// </summary>
        private ApMoveDestinationProxy TargetProxy { get; set; }
        //private AutoPilotDestinationProxy TargetProxy { get; set; }

        private string TargetFullName {
            get { return TargetProxy != null ? TargetProxy.Destination.DebugName : "No ApTargetProxy"; }
        }

        /// <summary>
        /// Distance from this AutoPilot's client to the TargetPoint.
        /// </summary>
        private float TargetDistance { get { return Vector3.Distance(Position, TargetProxy.Position); } }

        internal IShip Ship { get { return _ship; } }


        /// <summary>
        /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
        /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
        /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
        /// </remarks>
        /// </summary>
        private Action _actionToExecuteWhenFleetIsAligned;
        private CourseRefreshMode _obstacleFoundCourseRefreshMode;

        private ApObstacleCheckTask_Old _obstacleCheckTask;
        private ApMoveTask_Old _moveTask;
        private ApMaintainPositionTask_Old _maintainPositionTask;

        private EngineRoom _engineRoom;
        private ShipHelm _helm;

        private IShip _ship;
        private Transform _shipTransform;
        private ApDirective _directive;

        public AutoPilot_Old(ShipHelm helm, EngineRoom engineRoom, IShip ship, Transform shipTransform) {
            _helm = helm;
            _engineRoom = engineRoom;
            _ship = ship;
            _shipTransform = shipTransform;
        }

        /// <summary>
        /// Engages the pilot to move to the target using the provided proxy. It will notify the ship
        /// when it arrives via Ship.HandleTargetReached.
        /// </summary>
        /// <param name="directive">The AutoPilot Directive.</param>
        /// <param name="tgtProxy">The proxy for the target this Pilot is being engaged to reach.</param>
        /// <param name="speed">The initial speed the AutoPilot should travel at.</param>
        internal void Engage(ApDirective directive, ApMoveDestinationProxy tgtProxy, Speed speed) {
            Utility.ValidateNotNull(tgtProxy);
            D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
            D.Assert(!IsEngaged);
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);

            if (directive == ApDirective.Strafe) {
                D.Assert(tgtProxy is ApStrafeDestinationProxy);
            }

            if (directive == ApDirective.Bombard) {
                D.Assert(tgtProxy is ApBesiegeDestinationProxy);
            }


            _directive = directive;
            TargetProxy = tgtProxy;
            ApSpeedSetting = speed;
            IsCurrentSpeedFleetwide = directive == ApDirective.MoveToAsFleet;
            IsEngaged = true;

            // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
            // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
            if (TargetProxy.HasArrived) {
                D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, TargetProxy.DebugName);
                HandleTargetReached();
                return;
            }
            if (ShowDebugLog && TargetDistance < TargetProxy.InnerRadius) {
                D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, TargetProxy.DebugName);
            }

            if (IsAttacking) {
                D.Assert(IsCmdWithinRangeToSupportAttackOnTarget, DebugName); // primary target picked should qualify
            }

            if (_moveTask == null) {
                _moveTask = new ApMoveTask_Old(this);
                // _apMoveTask.hasArrivedOneShot event subscribed to in InitiateNavigationTo
            }

            if (_obstacleCheckTask == null) {
                _obstacleCheckTask = new ApObstacleCheckTask_Old(this);    // made here so can be used below
                _obstacleCheckTask.obstacleFound += ObstacleFoundEventHandler;
            }

            RefreshCourse(CourseRefreshMode.NewCourse);
            ApMoveDestinationProxy detour;
            if (_obstacleCheckTask.TryCheckForObstacleEnrouteTo(TargetProxy, out detour)) {
                RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
                InitiateCourseToTargetVia(detour);
            }
            else {
                InitiateDirectCourseToTarget();
            }
        }
        //internal void Engage(ApDirective directive, AutoPilotDestinationProxy tgtProxy, Speed speed) {
        //    Utility.ValidateNotNull(tgtProxy);
        //    D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
        //    D.Assert(!IsEngaged);
        //    D.AssertNull(_actionToExecuteWhenFleetIsAligned);

        //    _directive = directive;
        //    TargetProxy = tgtProxy;
        //    ApSpeedSetting = speed;
        //    IsCurrentSpeedFleetwide = directive == ApDirective.MoveToAsFleet;
        //    IsEngaged = true;

        //    // Note: Now OK to test for arrival here as WaitForFleetToAlign only waits for ship's that have registered their delegate.
        //    // There is no longer any reason for WaitForFleetToAlign to warn if delegate count < Element count.
        //    if (TargetProxy.HasArrived(Position)) {
        //        D.Log(ShowDebugLog, "{0} has already arrived! It is engaging Pilot from within {1}.", DebugName, TargetProxy.DebugName);
        //        HandleTargetReached();
        //        return;
        //    }
        //    if (ShowDebugLog && TargetDistance < TargetProxy.InnerRadius) {
        //        D.LogBold("{0} is inside {1}.InnerRadius!", DebugName, TargetProxy.DebugName);
        //    }

        //    if (IsAttacking) {
        //        D.Assert(IsCmdWithinRangeToSupportAttackOnTarget, DebugName); // primary target picked should qualify
        //    }

        //    if (_moveTask == null) {
        //        _moveTask = new ApMoveTask(this);
        //        // _apMoveTask.hasArrivedOneShot event subscribed to in InitiateNavigationTo
        //    }

        //    if (_obstacleCheckTask == null) {
        //        _obstacleCheckTask = new ApObstacleCheckTask(this);    // made here so can be used below
        //        _obstacleCheckTask.obstacleFound += ObstacleFoundEventHandler;
        //    }

        //    RefreshCourse(CourseRefreshMode.NewCourse);
        //    AutoPilotDestinationProxy detour;
        //    if (_obstacleCheckTask.TryCheckForObstacleEnrouteTo(TargetProxy, out detour)) {
        //        RefreshCourse(CourseRefreshMode.AddWaypoint, detour);
        //        InitiateCourseToTargetVia(detour);
        //    }
        //    else {
        //        InitiateDirectCourseToTarget();
        //    }
        //}

        /// <summary>
        /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
        /// 1) It may wait for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        private void InitiateDirectCourseToTarget() {
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} beginning prep to initiate direct course to {1} at {2}. \nDistance to target = {3:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, ApTargetDistance);

            Vector3 targetBearing = (TargetProxy.Position - Position).normalized;
            if (targetBearing.IsSameAs(Vector3.zero)) {
                D.Error("{0} ordered to move to target {1} at same location. This should be filtered out by EngagePilot().", DebugName, TargetFullName);
            }
            if (IsFleetwideMove) {
                ChangeHeading(targetBearing, eliminateDrift: true);

                _actionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for target {2}.", DebugName, _ship.Command.Name, ApTargetFullName);
                    _actionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: true);
                    bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading(targetBearing, eliminateDrift: true, headingConfirmed: () => {
                    //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
                    EngageEnginesAtApSpeed(isFleetSpeed: false);
                    bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                        HandleTargetReached();
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                    }
                });
            }
        }

        /// <summary>
        /// Initiates a course to the target after first going to <c>obstacleDetour</c>. This 'Initiate' version includes 2 responsibilities
        /// not present in the 'Continue' version. 1) It may wait for the fleet to align before departure, and 2) engages the engines.
        /// </summary>
        /// <param name="obstacleDetour">The proxy for the obstacle detour.</param>
        private void InitiateCourseToTargetVia(ApMoveDestinationProxy obstacleDetour) {
            D.AssertNull(_actionToExecuteWhenFleetIsAligned);
            //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
            //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

            Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
            if (newHeading.IsSameAs(Vector3.zero)) {
                D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
            }
            if (IsFleetwideMove) {
                ChangeHeading(newHeading, eliminateDrift: true);

                _actionToExecuteWhenFleetIsAligned = () => {
                    //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
                    //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
                    _actionToExecuteWhenFleetIsAligned = null;
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        ResumeDirectCourseToTarget(eliminateDrift: true);
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                };
                //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
                _ship.Command.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned, _ship);
            }
            else {
                ChangeHeading(newHeading, eliminateDrift: true, headingConfirmed: () => {
                    EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                                   // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                    bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                        RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                        bool eliminateDrift = _directive == ApDirective.Strafe ? false : true;
                        ResumeDirectCourseToTarget(eliminateDrift);
                    });
                    if (isNavInitiated) {
                        InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                    }
                });
            }
        }
        //private void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
        //    D.AssertNull(_actionToExecuteWhenFleetIsAligned);
        //    //D.Log(ShowDebugLog, "{0} initiating course to target {1} at {2} via obstacle detour {3}. Distance to detour = {4:0.0}.",
        //    //Name, TargetFullName, ApTargetProxy.Position, obstacleDetour.DebugName, Vector3.Distance(Position, obstacleDetour.Position));

        //    Vector3 newHeading = (obstacleDetour.Position - Position).normalized;
        //    if (newHeading.IsSameAs(Vector3.zero)) {
        //        D.Error("{0}: ObstacleDetour and current location shouldn't be able to be the same.", DebugName);
        //    }
        //    if (IsFleetwideMove) {
        //        ChangeHeading(newHeading, eliminateDrift: true);

        //        _actionToExecuteWhenFleetIsAligned = () => {
        //            //D.Log(ShowDebugLog, "{0} reports fleet {1} is aligned. Initiating departure for detour {2}.",
        //            //Name, _ship.Command.DisplayName, obstacleDetour.DebugName);
        //            _actionToExecuteWhenFleetIsAligned = null;
        //            EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
        //                                                           // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target

        //            bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
        //                RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
        //                ResumeDirectCourseToTarget(eliminateDrift: true);
        //            });
        //            if (isNavInitiated) {
        //                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
        //            }
        //        };
        //        //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
        //        _ship.Command.WaitForFleetToAlign(_actionToExecuteWhenFleetIsAligned, _ship);
        //    }
        //    else {
        //        ChangeHeading(newHeading, eliminateDrift: true, headingConfirmed: () => {
        //            EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
        //                                                           // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
        //            bool isNavInitiated = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
        //                RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
        //                bool eliminateDrift = _directive == ApDirective.Strafe ? false : true;
        //                ResumeDirectCourseToTarget(eliminateDrift);
        //            });
        //            if (isNavInitiated) {
        //                InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
        //            }
        //        });
        //    }
        //}

        /// <summary>
        /// Resumes a direct course to target. Called while underway upon arrival at a strafing waypoint or an obstacle detour.
        /// Unlike the 'Initiate' version, this method neither waits for the rest of the fleet, nor engages the engines since they are already engaged.
        /// </summary>
        /// <param name="eliminateDrift">if set to <c>true</c> drift is eliminated upon completion of a turn.</param>
        private void ResumeDirectCourseToTarget(bool eliminateDrift) {
            D.Assert(IsEngaged);
            ResetTasks();
            //D.Log(ShowDebugLog, "{0} beginning prep to resume direct course to {1} at {2}. \nDistance to target = {3:0.#}.",
            //    DebugName, TargetFullName, TargetProxy.Position, TargetDistance);

            if (IsStrafeAttacking) {
                (TargetProxy as ApStrafeDestinationProxy).RefreshStrafePosition();
            }

            ResumeApSpeed();    // CurrentSpeed can be slow coming out of a detour, also uses ShipSpeed to catchup
            Vector3 targetBearing = (TargetProxy.Position - Position).normalized;
            ChangeHeading(targetBearing, eliminateDrift, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading toward {1}.", DebugName, TargetFullName);
                bool isNavInitiated = InitiateNavigationTo(TargetProxy, hasArrived: () => {
                    HandleTargetReached();
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(TargetProxy, CourseRefreshMode.AddWaypoint);
                }
            });
        }

        /// <summary>
        /// Continues the course to target via the provided waypoint. 
        /// <remarsk>Called while underway upon encountering an obstacle or having picked a strafing wayPoint.</remarsk>
        /// </summary>
        /// <param name="wayPtProxy">The proxy for the Waypoint.</param>
        /// <param name="eliminateDrift">if set to <c>true</c> this setting eliminates drift after turns are complete.</param>
        private void ContinueCourseToTargetVia(ApMoveDestinationProxy wayPtProxy, bool eliminateDrift) {
            D.Assert(IsEngaged);
            ResetTasks();
            //D.Log(ShowDebugLog, "{0} continuing course to target {1} via Waypoint {2}. Distance to Waypoint = {3:0.0}.",
            //    DebugName, TargetFullName, wayPtProxy.DebugName, Vector3.Distance(Position, wayPtProxy.Position));

            ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
            Vector3 newHeading = (wayPtProxy.Position - Position).normalized;
            ChangeHeading(newHeading, eliminateDrift, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is now on heading to reach waypoint {1}.", DebugName, wayPtProxy.DebugName);
                bool isNavInitiated = InitiateNavigationTo(wayPtProxy, hasArrived: () => {
                    // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, wayPtProxy);
                    ResumeDirectCourseToTarget(eliminateDrift);
                });
                if (isNavInitiated) {
                    InitiateObstacleCheckingEnrouteTo(wayPtProxy, CourseRefreshMode.ReplaceObstacleDetour);
                }
            });
        }
        //private void ContinueCourseToTargetVia(AutoPilotDestinationProxy wayPtProxy, bool eliminateDrift) {
        //    D.Assert(IsEngaged);
        //    ResetTasks();
        //    //D.Log(ShowDebugLog, "{0} continuing course to target {1} via Waypoint {2}. Distance to Waypoint = {3:0.0}.",
        //    //    DebugName, TargetFullName, wayPtProxy.DebugName, Vector3.Distance(Position, wayPtProxy.Position));

        //    ResumeApSpeed(); // Uses ShipSpeed to catchup as we must go through this detour
        //    Vector3 newHeading = (wayPtProxy.Position - Position).normalized;
        //    ChangeHeading(newHeading, eliminateDrift, headingConfirmed: () => {
        //        //D.Log(ShowDebugLog, "{0} is now on heading to reach waypoint {1}.", DebugName, wayPtProxy.DebugName);
        //        bool isNavInitiated = InitiateNavigationTo(wayPtProxy, hasArrived: () => {
        //            // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then direct to target
        //            RefreshCourse(CourseRefreshMode.RemoveWaypoint, wayPtProxy);
        //            ResumeDirectCourseToTarget(eliminateDrift);
        //        });
        //        if (isNavInitiated) {
        //            InitiateObstacleCheckingEnrouteTo(wayPtProxy, CourseRefreshMode.ReplaceObstacleDetour);
        //        }
        //    });
        //}

        /// <summary>
        /// Initiates navigation to the destination indicated by destProxy, returning <c>true</c> if navigation was initiated,
        /// <c>false</c> if navigation is not needed since already present at destination.
        /// </summary>
        /// <param name="destProxy">The destination proxy.</param>
        /// <param name="hasArrived">Delegate executed when the ship has arrived at the destination.</param>
        /// <returns></returns>
        private bool InitiateNavigationTo(ApMoveDestinationProxy destProxy, Action hasArrived) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            if (!_engineRoom.IsPropulsionEngaged) {
                D.Error("{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}", DebugName, destProxy.DebugName, ApSpeedSetting.GetValueName());
            }

            float distanceToArrival;
            Vector3 directionToArrival;
#pragma warning disable 0219
            bool isArrived = false;
#pragma warning restore 0219
            if (isArrived = !destProxy.TryCheckProgress(out directionToArrival, out distanceToArrival)) {
                // arrived
                hasArrived();
                return false;   // already arrived so nav not initiated
            }
            else {
                bool isDestinationADetour = destProxy != TargetProxy;
                bool isDestFastMover = destProxy.IsFastMover;
                IsIncreaseAboveApSpeedSettingAllowed = isDestinationADetour || isDestFastMover;

                _moveTask.hasArrivedOneShot += (sender, EventArgs) => hasArrived();
                _moveTask.Execute(destProxy);
                return true;
            }
        }
        //        private bool InitiateNavigationTo(AutoPilotDestinationProxy destProxy, Action hasArrived) {
        //            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        //            if (!_engineRoom.IsPropulsionEngaged) {
        //                D.Error("{0}.InitiateNavigationTo({1}) called without propulsion engaged. AutoPilotSpeed: {2}", DebugName, destProxy.DebugName, ApSpeedSetting.GetValueName());
        //            }

        //            float distanceToArrival;
        //            Vector3 directionToArrival;
        //#pragma warning disable 0219
        //            bool isArrived = false;
        //#pragma warning restore 0219
        //            if (isArrived = !destProxy.TryGetArrivalDistanceAndDirection(Position, out directionToArrival, out distanceToArrival)) {
        //                // arrived
        //                hasArrived();
        //                return false;   // already arrived so nav not initiated
        //            }
        //            else {
        //                bool isDestinationADetour = destProxy != TargetProxy;
        //                bool isDestFastMover = destProxy.IsFastMover;
        //                IsIncreaseAboveApSpeedSettingAllowed = isDestinationADetour || isDestFastMover;

        //                _moveTask.hasArrivedOneShot += (sender, EventArgs) => hasArrived();
        //                _moveTask.Execute(destProxy);
        //                return true;
        //            }
        //        }

        #region Strafing

        private ApMoveDestinationProxy GenerateStrafingWaypoint() {
            var directionToShipFromTgt = (Position - TargetProxy.Position).normalized;
            var waypointVector = CurrentHeading + directionToShipFromTgt;   // HACK
            Vector3 waypointDirectionFromTgt = waypointVector != Vector3.zero ? waypointVector.normalized : directionToShipFromTgt;
            float waypointDistanceFromTgt = TargetProxy.OuterRadius * 2F;
            var waypointPosition = TargetProxy.Position + waypointDirectionFromTgt * waypointDistanceFromTgt;
            var waypoint = new StationaryLocation(waypointPosition);
            return waypoint.GetApMoveTgtProxy(Vector3.zero, Constants.ZeroF, _ship);
        }
        //private AutoPilotDestinationProxy GenerateStrafingWaypoint() {
        //    var directionToShipFromTgt = (Position - TargetProxy.Position).normalized;
        //    var waypointVector = CurrentHeading + directionToShipFromTgt;   // HACK
        //    Vector3 waypointDirectionFromTgt = waypointVector != Vector3.zero ? waypointVector.normalized : directionToShipFromTgt;
        //    float waypointDistanceFromTgt = TargetProxy.OuterRadius * 2F;
        //    var waypointPosition = TargetProxy.Position + waypointDirectionFromTgt * waypointDistanceFromTgt;
        //    var waypoint = new StationaryLocation(waypointPosition);
        //    return waypoint.GetApMoveTgtProxy(Vector3.zero, Constants.ZeroF, Position);
        //}

        #endregion

        #region Obstacle Checking

        private void InitiateObstacleCheckingEnrouteTo(ApMoveDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
            _obstacleFoundCourseRefreshMode = courseRefreshMode;
            _obstacleCheckTask.Execute(destProxy);
        }
        //private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy, CourseRefreshMode courseRefreshMode) {
        //    _obstacleFoundCourseRefreshMode = courseRefreshMode;
        //    _obstacleCheckTask.Execute(destProxy);
        //}

        private void ObstacleFoundEventHandler(object sender, ApObstacleCheckTask_Old.ObstacleFoundEventArgs e) {
            HandleObstacleFound(e.DetourProxy);
        }

        private void HandleObstacleFound(ApMoveDestinationProxy detourProxy) {
            RefreshCourse(_obstacleFoundCourseRefreshMode, detourProxy);
            _obstacleFoundCourseRefreshMode = default(CourseRefreshMode);
            ContinueCourseToTargetVia(detourProxy, eliminateDrift: true);
        }
        //private void HandleObstacleFound(AutoPilotDestinationProxy detourProxy) {
        //    RefreshCourse(_obstacleFoundCourseRefreshMode, detourProxy);
        //    _obstacleFoundCourseRefreshMode = default(CourseRefreshMode);
        //    ContinueCourseToTargetVia(detourProxy, eliminateDrift: true);
        //}

        internal void HandleObstacleFoundIsTarget(IAvoidableObstacle obstacle) {
            if (_ship.IsHQ) {
                // should never happen as HQ approach is always direct            
                D.Warn("HQ {0} encountered obstacle {1} which is target.", DebugName, obstacle.DebugName);
            }
            TargetProxy.ResetOffset();   // go directly to target

            D.Assert(IsEngaged);
            ResumeDirectCourseToTarget(eliminateDrift: true);
        }

        #endregion

        #region Bombarding

        private void MaintainBombardPosition() {
            if (_maintainPositionTask == null) {
                _maintainPositionTask = new ApMaintainPositionTask_Old(this);
                _maintainPositionTask.lostPosition += BombardPositionLostEventHandler;
            }
            ResetTasks();
            _maintainPositionTask.Execute(TargetProxy as ApBesiegeDestinationProxy);
        }
        //private void MaintainBombardPosition() {
        //    if (_maintainPositionTask == null) {
        //        _maintainPositionTask = new ApMaintainPositionTask(this);
        //        _maintainPositionTask.lostPosition += BombardPositionLostEventHandler;
        //    }
        //    ResetTasks();
        //    _maintainPositionTask.Execute(TargetProxy);
        //}

        private void BombardPositionLostEventHandler(object sender, EventArgs e) {
            HandleBombardPositionLost();
        }

        private void HandleBombardPositionLost() {
            D.Assert(IsEngaged, "{0} lost Bombard Position on {1} during Frame {2} after being disengaged on Frame {3}."
                .Inject(DebugName, TargetFullName, Time.frameCount, __lastFrameDisengaged));
            RefreshCourse(CourseRefreshMode.NewCourse);
            ResumeDirectCourseToTarget(eliminateDrift: true);
        }

        #endregion

        private void HandleTargetReached() {
            D.AssertNotDefault((int)_directive);
            D.Assert(IsEngaged, "{0}.HandleTargetReached() called during Frame {1} after being disengaged on Frame {2}.".Inject(DebugName, Time.frameCount, __lastFrameDisengaged));

            D.Log(ShowDebugLog, "{0} at {1} has reached {2} \nat {3}. Actual proximity: {4:0.0000} units.", DebugName, Position, TargetFullName, TargetProxy.Position, TargetDistance);
            if (IsAttacking) {
                if (!IsCmdWithinRangeToSupportAttackOnTarget) {
                    HandleTgtUncatchable();
                    return;
                }
                if (_directive == ApDirective.Bombard) {
                    RefreshCourse(CourseRefreshMode.ClearCourse);
                    D.Log(ShowDebugLog, "{0} is bombarding {1}.", DebugName, TargetFullName);
                    MaintainBombardPosition();
                }
                else {
                    D.AssertEqual(ApDirective.Strafe, _directive);
                    D.Log(ShowDebugLog, "{0} is strafing {1}.", DebugName, TargetFullName);
                    RefreshCourse(CourseRefreshMode.NewCourse);
                    var beginRunWaypoint = (TargetProxy as ApStrafeDestinationProxy).GenerateBeginRunWaypoint();
                    RefreshCourse(CourseRefreshMode.AddWaypoint, beginRunWaypoint);
                    ContinueCourseToTargetVia(beginRunWaypoint, eliminateDrift: false);
                }
            }
            else {
                RefreshCourse(CourseRefreshMode.ClearCourse);
                _helm.HandleApTargetReached();
            }
        }

        internal void HandleCourseChanged() {
            _helm.HandleApCourseChanged();
        }

        internal void HandleTgtUncatchable() {  // 3.1.17 Used by MoveApTask
            D.Log(ShowDebugLog, "{0} is getting too far from Cmd in pursuit of {1} which is now deemed uncatchable.", _ship.DebugName, TargetFullName);
            RefreshCourse(CourseRefreshMode.ClearCourse);
            _helm.HandleApTargetUncatchable();
        }

        /// <summary>
        /// Varies the check period by plus or minus 10% to spread out recurring event firing.
        /// </summary>
        /// <param name="hoursPerCheckPeriod">The hours per check period.</param>
        /// <returns></returns>
        internal float VaryCheckPeriod(float hoursPerCheckPeriod) {
            return UnityEngine.Random.Range(hoursPerCheckPeriod * 0.9F, hoursPerCheckPeriod * 1.1F);
        }

        internal void ChangeHeading(Vector3 newHeading, bool eliminateDrift, Action headingConfirmed = null) {
            _helm.ChangeHeading(newHeading, eliminateDrift, headingConfirmed);
        }

        internal void ChangeSpeed(Speed speed, bool isFleetSpeed) {
            IsCurrentSpeedFleetwide = isFleetSpeed;
            _helm.ChangeSpeed(speed, isFleetSpeed);
        }

        internal float GetSpeedValue(Speed speed) {
            return IsCurrentSpeedFleetwide ? speed.GetUnitsPerHour(_ship.Command.UnitFullSpeedValue) : speed.GetUnitsPerHour(_helm.FullSpeedValue);
        }

        /// <summary>
        /// Used by the Pilot to initially engage the engines at ApSpeed.
        /// </summary>
        /// <param name="isFleetSpeed">if set to <c>true</c> [is fleet speed].</param>
        private void EngageEnginesAtApSpeed(bool isFleetSpeed) {
            D.Assert(IsEngaged);
            //D.Log(ShowDebugLog, "{0} is engaging engines at speed {1}.", DebugName, ApSpeedSetting.GetValueName());
            ChangeSpeed(ApSpeedSetting, isFleetSpeed);
        }

        /// <summary>
        /// Used by the Pilot to resume ApSpeed going into or coming out of a detour course leg.
        /// </summary>
        private void ResumeApSpeed() {
            D.Assert(IsEngaged);
            //D.Log(ShowDebugLog, "{0} is resuming speed {1}.", DebugName, ApSpeedSetting.GetValueName());
            ChangeSpeed(ApSpeedSetting, isFleetSpeed: false);
        }

        /// <summary>
        /// Refreshes the course.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="wayPtProxy">The optional waypoint. When not null, this is always a StationaryLocation detour to avoid an obstacle.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshCourse(CourseRefreshMode mode, ApMoveDestinationProxy wayPtProxy = null) {
            //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), AutoPilotCourse.Count);
            IList<IShipNavigable> apCourse = _helm.ApCourse;
            switch (mode) {
                case CourseRefreshMode.NewCourse:
                    D.AssertNull(wayPtProxy);
                    apCourse.Clear();
                    apCourse.Add(_ship as IShipNavigable);
                    IShipNavigable courseTgt;
                    if (TargetProxy.IsMobile) {
                        courseTgt = new MobileLocation(new Reference<Vector3>(() => TargetProxy.Position));
                    }
                    else {
                        courseTgt = new StationaryLocation(TargetProxy.Position);
                    }
                    apCourse.Add(courseTgt);  // includes fstOffset
                    break;
                case CourseRefreshMode.AddWaypoint:
                    D.AssertEqual(2, apCourse.Count, DebugName);
                    apCourse.Insert(apCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.ReplaceObstacleDetour:
                    D.AssertEqual(3, apCourse.Count, DebugName);
                    apCourse.RemoveAt(apCourse.Count - 2);          // changes Course.Count
                    apCourse.Insert(apCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
                    break;
                case CourseRefreshMode.RemoveWaypoint:
                    D.AssertEqual(3, apCourse.Count, DebugName);
                    bool isRemoved = apCourse.Remove(new StationaryLocation(wayPtProxy.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
                    D.Assert(isRemoved);
                    break;
                case CourseRefreshMode.ClearCourse:
                    D.AssertNull(wayPtProxy);
                    apCourse.Clear();
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
            }
            //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
            HandleCourseChanged();
        }
        //private void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy wayPtProxy = null) {
        //    //D.Log(ShowDebugLog, "{0}.RefreshCourse() called. Mode = {1}. CourseCountBefore = {2}.", DebugName, mode.GetValueName(), AutoPilotCourse.Count);
        //    IList<IShipNavigable> apCourse = _helm.ApCourse;
        //    switch (mode) {
        //        case CourseRefreshMode.NewCourse:
        //            D.AssertNull(wayPtProxy);
        //            apCourse.Clear();
        //            apCourse.Add(_ship as IShipNavigable);
        //            IShipNavigable courseTgt;
        //            if (TargetProxy.IsMobile) {
        //                courseTgt = new MobileLocation(new Reference<Vector3>(() => TargetProxy.Position));
        //            }
        //            else {
        //                courseTgt = new StationaryLocation(TargetProxy.Position);
        //            }
        //            apCourse.Add(courseTgt);  // includes fstOffset
        //            break;
        //        case CourseRefreshMode.AddWaypoint:
        //            D.AssertEqual(2, apCourse.Count, DebugName);
        //            apCourse.Insert(apCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
        //            break;
        //        case CourseRefreshMode.ReplaceObstacleDetour:
        //            D.AssertEqual(3, apCourse.Count, DebugName);
        //            apCourse.RemoveAt(apCourse.Count - 2);          // changes Course.Count
        //            apCourse.Insert(apCourse.Count - 1, new StationaryLocation(wayPtProxy.Position));    // changes Course.Count
        //            break;
        //        case CourseRefreshMode.RemoveWaypoint:
        //            D.AssertEqual(3, apCourse.Count, DebugName);
        //            bool isRemoved = apCourse.Remove(new StationaryLocation(wayPtProxy.Position));     // Course.RemoveAt(Course.Count - 2);  // changes Course.Count
        //            D.Assert(isRemoved);
        //            break;
        //        case CourseRefreshMode.ClearCourse:
        //            D.AssertNull(wayPtProxy);
        //            apCourse.Clear();
        //            break;
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
        //    }
        //    //D.Log(ShowDebugLog, "CourseCountAfter = {0}.", Course.Count);
        //    HandleCourseChanged();
        //}

        internal void Disengage() {
            ResetTasks();
            IsEngaged = false;
            RefreshCourse(CourseRefreshMode.ClearCourse);
            ApSpeedSetting = Speed.None;
            TargetProxy = null;
            _directive = ApDirective.None;
            IsCurrentSpeedFleetwide = false;
            IsIncreaseAboveApSpeedSettingAllowed = false;
            _doesMoveTaskProgressCheckPeriodNeedRefresh = false;
            // DoesObstacleCheckPeriodNeedRefresh handled by _obstacleCheckTask.ResetForReuse
            _obstacleFoundCourseRefreshMode = default(CourseRefreshMode);
            __lastFrameDisengaged = Time.frameCount;
        }

        private void ResetTasks() {
            if (_moveTask != null) {
                _moveTask.ResetForReuse();
            }
            if (_obstacleCheckTask != null) {
                _obstacleCheckTask.ResetForReuse();
            }
            if (_maintainPositionTask != null) {
                _maintainPositionTask.ResetForReuse();
            }
            if (_actionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_actionToExecuteWhenFleetIsAligned, _ship);
                _actionToExecuteWhenFleetIsAligned = null;
            }
        }

        #region Cleanup

        private void Cleanup() {
            Unsubscribe();
            if (_moveTask != null) {
                _moveTask.Dispose();
            }
            if (_obstacleCheckTask != null) {
                _obstacleCheckTask.Dispose();
            }
            if (_maintainPositionTask != null) {
                _maintainPositionTask.Dispose();
            }
        }

        private void Unsubscribe() {
            // unsubscribing to _apMoveTask.hasArrivedOneShot handled by _apMoveTask.Cleanup
            if (_obstacleCheckTask != null) {  // OPTIMIZE redundant as _apObstacleCheckTask.Cleanup handles?
                _obstacleCheckTask.obstacleFound -= ObstacleFoundEventHandler;
            }
            if (_maintainPositionTask != null) {
                _maintainPositionTask.lostPosition -= BombardPositionLostEventHandler;
            }
            if (_actionToExecuteWhenFleetIsAligned != null) {
                _ship.Command.RemoveFleetIsAlignedCallback(_actionToExecuteWhenFleetIsAligned, _ship);
                _actionToExecuteWhenFleetIsAligned = null;
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// The frame number this AutoPilot was last disengaged.
        /// </summary>
        private int __lastFrameDisengaged;

        #endregion

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        /// <summary>
        /// Directives used to instruct the AutoPilot.
        /// </summary>
        public enum ApDirective {

            None,

            /// <summary>
            /// Move to a target as an individual ship.
            /// </summary>
            MoveTo,

            /// <summary>
            /// Move to a target as part of a fleet, aka move is fleetwide...
            /// </summary>
            MoveToAsFleet,

            /// <summary>
            /// Move to a target and maintain position within the target's arrival window.
            /// </summary>
            Bombard,

            Strafe
        }

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

