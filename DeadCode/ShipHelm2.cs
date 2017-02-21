// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHelm2.cs
// Navigation, Heading and Speed control for a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Navigation, Heading and Speed control for a ship.
/// </summary>
[Obsolete]
public class ShipHelm2 : AShipHelm {

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

    internal new bool IsPilotEngaged { get { return base.IsPilotEngaged; } }

    /// <summary>
    /// Indicates whether the ship is actively moving under power. <c>True</c> if under propulsion
    /// or turning, <c>false</c> otherwise, including when still retaining some residual velocity.
    /// </summary>
    internal new bool IsActivelyUnderway { get { return base.IsActivelyUnderway; } }

    protected override float ReqdObstacleClearanceRadius {
        get { return _isApFleetwideMove ? _ship.Command.UnitMaxFormationRadius : _ship.CollisionDetectionZoneRadius; }
    }

    /// <summary>
    /// Indicates whether this is a coordinated fleet move or a move by the ship on its own to the Target.
    /// A coordinated fleet move has the ship pay attention to fleet desires like a coordinated departure, 
    /// moving in formation and moving at speeds the whole fleet can maintain.
    /// </summary>
    private bool _isApFleetwideMove;

    /// <summary>
    /// Delegate pointing to an anonymous method handling work after the fleet has aligned for departure.
    /// <remarks>This reference is necessary to allow removal of the callback from Fleet.WaitForFleetToAlign()
    /// in cases where the AutoPilot is disengaged while waiting for the fleet to align. Delegate.Target.Type = ShipHelm.
    /// </remarks>
    /// </summary>
    private Action _apActionToExecuteWhenFleetIsAligned;

    private IList<IDisposable> _subscriptions;

    #region Initialization

    /// <summary>
    /// Initializes a new instance of the <see cref="ShipHelm" /> class.
    /// </summary>
    /// <param name="ship">The ship.</param>
    /// <param name="shipRigidbody">The ship rigidbody.</param>
    public ShipHelm2(ShipItem ship, EngineRoom engineRoom) : base(ship, engineRoom) {
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
    internal void EngagePilotToMoveTo(AutoPilotDestinationProxy apTgtProxy, Speed speed, bool isFleetwideMove) {
        Utility.ValidateNotNull(apTgtProxy);
        D.Assert(!InvalidApSpeeds.Contains(speed), speed.GetValueName());
        ApTargetProxy = apTgtProxy;
        ApSpeed = speed;
        _isApFleetwideMove = isFleetwideMove;
        IsApCurrentSpeedFleetwide = isFleetwideMove;
        RefreshCourse(CourseRefreshMode.NewCourse);
        EngagePilot();
    }

    #endregion

    #region Course Navigation

    /// <summary>
    /// Initiates a direct course to target. This 'Initiate' version includes 2 responsibilities not present in the 'Resume' version.
    /// 1) It waits for the fleet to align before departure, and 2) engages the engines.
    /// </summary>
    protected override void InitiateDirectCourseToTarget() {
        D.AssertNull(_apNavJob);
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
                bool isAlreadyArrived = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                    HandleTargetReached();
                });
                if (!isAlreadyArrived) {
                    InitiateObstacleCheckingEnrouteTo(ApTargetProxy, CourseRefreshMode.AddWaypoint);
                }
            };
            //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
            _ship.Command.WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
        }
        else {
            ChangeHeading_Internal(targetBearing, headingConfirmed: () => {
                //D.Log(ShowDebugLog, "{0} is initiating direct course to {1} in Frame {2}.", DebugName, ApTargetFullName, Time.frameCount);
                EngageEnginesAtApSpeed(isFleetSpeed: false);
                bool isAlreadyArrived = InitiateNavigationTo(ApTargetProxy, hasArrived: () => {
                    HandleTargetReached();
                });
                if (!isAlreadyArrived) {
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
    protected override void InitiateCourseToTargetVia(AutoPilotDestinationProxy obstacleDetour) {
        D.AssertNull(_apNavJob);
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

                bool isAlreadyArrived = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                if (!isAlreadyArrived) {
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                }
            };
            //D.Log(ShowDebugLog, "{0} starting wait for fleet to align, actual speed = {1:0.##}.", DebugName, ActualSpeedValue);
            _ship.Command.WaitForFleetToAlign(_apActionToExecuteWhenFleetIsAligned, _ship);
        }
        else {
            ChangeHeading_Internal(newHeading, headingConfirmed: () => {
                EngageEnginesAtApSpeed(isFleetSpeed: false);   // this is a detour so catch up
                                                               // even if this is an obstacle that has appeared on the way to another obstacle detour, go around it, then try direct to target
                bool isAlreadyArrived = InitiateNavigationTo(obstacleDetour, hasArrived: () => {
                    RefreshCourse(CourseRefreshMode.RemoveWaypoint, obstacleDetour);
                    ResumeDirectCourseToTarget();
                });
                if (!isAlreadyArrived) {
                    InitiateObstacleCheckingEnrouteTo(obstacleDetour, CourseRefreshMode.ReplaceObstacleDetour);
                }
            });
        }
    }

    /// <summary>
    /// Calculates and returns the world space offset to the provided detour that when combined with the
    /// detour's position, represents the actual location in world space this ship is trying to reach, 
    /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
    /// </summary>
    /// <param name="detour">The detour.</param>
    /// <returns></returns>
    protected override Vector3 CalcDetourOffset(StationaryLocation detour) {
        if (!_isApFleetwideMove) {
            return base.CalcDetourOffset(detour);
        }
        // make separate detour offsets as there may be a lot of ships encountering this detour
        Quaternion shipCurrentRotation = _shipTransform.rotation;
        Vector3 shipToDetourDirection = (detour.Position - _ship.Position).normalized;
        Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(_ship.CurrentHeading, shipToDetourDirection);
        Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
        Vector3 shipLocalFormationOffset = _ship.FormationStation.LocalOffset;
        Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, shipLocalFormationOffset);
        return detourWorldSpaceOffset;
    }

    #endregion

    #region Change Heading

    /// <summary>
    /// Primary exposed control that changes the direction the ship is headed and disengages the auto pilot.
    /// For use when managing the heading of the ship without using the Autopilot.
    /// </summary>
    /// <param name="newHeading">The new direction in world coordinates, normalized.</param>
    /// <param name="headingConfirmed">Delegate that fires when the ship gets to the new heading.</param>
    internal void ChangeHeading(Vector3 newHeading, Action headingConfirmed = null) {
        DisengagePilot(); // kills ChangeHeading job if pilot running
        if (IsTurnUnderway) {
            D.Warn("{0} received sequential ChangeHeading calls from Captain.", DebugName);
        }
        ChangeHeading_Internal(newHeading, headingConfirmed);
    }

    #endregion

    #region Change Speed

    /// <summary>
    /// Primary exposed control that changes the speed of the ship and disengages the pilot.
    /// For use when managing the speed of the ship without relying on  the Autopilot.
    /// </summary>
    /// <param name="newSpeed">The new speed.</param>
    internal void ChangeSpeed(Speed newSpeed) {
        D.Assert(__ValidExternalChangeSpeeds.Contains(newSpeed), newSpeed.GetValueName());
        //D.Log(ShowDebugLog, "{0} is about to disengage pilot and change speed to {1}.", DebugName, newSpeed.GetValueName());
        DisengagePilot();
        ChangeSpeed_Internal(newSpeed, isFleetSpeed: false);
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

    #endregion

    #region Event and Property Change Handlers

    private void FullSpeedPropChangedHandler() {
        HandleFullSpeedValueChanged();
    }

    // Note: No need for TopographyPropChangedHandler as FullSpeedValues get changed when density (and therefore CurrentDrag) changes
    // No need for GameSpeedPropChangedHandler as speedPerSec is no longer used

    #endregion

    /// <summary>
    /// Called when the ship 'arrives' at the Target.
    /// </summary>
    protected override void HandleTargetReached() {
        base.HandleTargetReached();
        _ship.HandleApTargetReached();
    }

    internal void HandleFleetFullSpeedValueChanged() {
        if (IsPilotEngaged) {
            if (IsApCurrentSpeedFleetwide) {
                // EngineRoom's CurrentSpeed is a FleetSpeed value so the Fleet's FullSpeed change will affect its value
                RefreshEngineRoomSpeedValues(isFleetSpeed: true);
                // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                _doesApProgressCheckPeriodNeedRefresh = true;
                _doesApObstacleCheckPeriodNeedRefresh = true;
            }
        }
    }

    private void HandleFullSpeedValueChanged() {
        if (IsPilotEngaged) {
            if (!IsApCurrentSpeedFleetwide) {
                // EngineRoom's CurrentSpeed is a ShipSpeed value so this Ship's FullSpeed change will affect its value
                RefreshEngineRoomSpeedValues(isFleetSpeed: false);
                // when CurrentSpeed values change as a result of a FullSpeed change, a refresh is needed
                _doesApProgressCheckPeriodNeedRefresh = true;
                _doesApObstacleCheckPeriodNeedRefresh = true;
            }
        }
        else if (_engineRoom.IsPropulsionEngaged) {
            // Propulsion is engaged and not by AutoPilot so must be external SpeedChange from Captain, value change will matter
            RefreshEngineRoomSpeedValues(isFleetSpeed: false);
        }
    }

    protected override void DisengagePilot_Internal() {
        base.DisengagePilot_Internal();
        _isApFleetwideMove = false;
    }


    #region Cleanup

    protected override void CleanupAnyRemainingJobs() {
        base.CleanupAnyRemainingJobs();
        if (_apActionToExecuteWhenFleetIsAligned != null) {
            _ship.Command.RemoveFleetIsAlignedCallback(_apActionToExecuteWhenFleetIsAligned, _ship);
            _apActionToExecuteWhenFleetIsAligned = null;
        }
    }

    protected override void Cleanup() {
        base.Cleanup();
        Unsubscribe();
    }

    private void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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



}

