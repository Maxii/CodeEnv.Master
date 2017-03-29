// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApObstacleCheckTask.cs
// AutoPilot task that checks for obstacles while moving to a target.
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
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// AutoPilot task that checks for obstacles while moving to a target.
    /// </summary>
    [Obsolete]
    public class ApObstacleCheckTask_Old : AApTask_Old {

        /// <summary>
        /// The turn angle threshold (in degrees) used to determine when a detour around an obstacle
        /// must be used. Logic: If the reqd turn to reach the detour is sharp (above this value), then
        /// we are either very close or the obstacle is very large so it is time to redirect around the obstacle.
        /// </summary>
        private const float DetourTurnAngleThreshold = 15F;

        private static readonly LayerMask AvoidableObstacleZoneOnlyLayerMask = LayerMaskUtility.CreateInclusiveMask(Layers.AvoidableObstacleZone);

        public event EventHandler<ObstacleFoundEventArgs> obstacleFound;

        public override bool IsEngaged { get { return _obstacleCheckJob != null; } }

        private bool _doesObstacleCheckPeriodNeedRefresh;
        internal bool DoesObstacleCheckPeriodNeedRefresh {
            get { return _doesObstacleCheckPeriodNeedRefresh; }
            set {
                D.Assert(IsEngaged);
                _doesObstacleCheckPeriodNeedRefresh = value;
            }
        }

        private Vector3 Position { get { return _autoPilot.Position; } }

        private GameTimeDuration _obstacleCheckJobPeriod;
        private Job _obstacleCheckJob;

        public ApObstacleCheckTask_Old(AutoPilot_Old autoPilot) : base(autoPilot) { }

        protected override void InitializeValuesAndReferences() {
            base.InitializeValuesAndReferences();
        }

        public void Execute(ApMoveDestinationProxy destProxy) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            InitiateObstacleCheckingEnrouteTo(destProxy);
        }
        //public override void Execute(AutoPilotDestinationProxy destProxy) {
        //    D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        //    InitiateObstacleCheckingEnrouteTo(destProxy);
        //}

        private void InitiateObstacleCheckingEnrouteTo(ApMoveDestinationProxy destProxy) {
            D.AssertNull(_obstacleCheckJob, DebugName);
            _obstacleCheckJobPeriod = __GenerateObstacleCheckJobPeriod();
            string jobName = "{0}.ApObstacleCheckJob".Inject(DebugName);
            _obstacleCheckJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => _obstacleCheckJobPeriod), jobName, waitMilestone: () => {
                ApMoveDestinationProxy detourProxy;
                if (TryCheckForObstacleEnrouteTo(destProxy, out detourProxy)) {
                    KillJob();
                    OnObstacleFound(detourProxy);
                    return;
                }
                if (DoesObstacleCheckPeriodNeedRefresh) {
                    _obstacleCheckJobPeriod = __GenerateObstacleCheckJobPeriod();
                    DoesObstacleCheckPeriodNeedRefresh = false;
                }
            });
        }
        //private void InitiateObstacleCheckingEnrouteTo(AutoPilotDestinationProxy destProxy) {
        //    D.AssertNull(_obstacleCheckJob, DebugName);
        //    _obstacleCheckJobPeriod = __GenerateObstacleCheckJobPeriod();
        //    string jobName = "{0}.ApObstacleCheckJob".Inject(DebugName);
        //    _obstacleCheckJob = _jobMgr.RecurringWaitForHours(new Reference<GameTimeDuration>(() => _obstacleCheckJobPeriod), jobName, waitMilestone: () => {
        //        AutoPilotDestinationProxy detourProxy;
        //        if (TryCheckForObstacleEnrouteTo(destProxy, out detourProxy)) {
        //            KillJob();
        //            OnObstacleFound(detourProxy);
        //            return;
        //        }
        //        if (DoesObstacleCheckPeriodNeedRefresh) {
        //            _obstacleCheckJobPeriod = __GenerateObstacleCheckJobPeriod();
        //            DoesObstacleCheckPeriodNeedRefresh = false;
        //        }
        //    });
        //}

        private GameTimeDuration __GenerateObstacleCheckJobPeriod() {
            float relativeObstacleFreq;  // IMPROVE OK for now as obstacleDensity is related but not same as Topography.GetRelativeDensity()
            float defaultHours;
            ValueRange<float> hoursRange;
            switch (_autoPilot.__Topography) {
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
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_autoPilot.__Topography));
            }
            float speedValue = _autoPilot.IntendedCurrentSpeedValue;
            float hoursBetweenChecks = speedValue > Constants.ZeroF ? relativeObstacleFreq / speedValue : defaultHours;
            hoursBetweenChecks = hoursRange.Clamp(hoursBetweenChecks);
            hoursBetweenChecks = _autoPilot.VaryCheckPeriod(hoursBetweenChecks);

            float checksPerHour = 1F / hoursBetweenChecks;
            if (checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond > GameReferences.FpsReadout.FramesPerSecond) {
                // check frequency is higher than the game engine can run
                D.Warn("{0} obstacleChecksPerSec {1:0.#} > FPS {2:0.#}.",
                    DebugName, checksPerHour * GameTime.Instance.GameSpeedAdjustedHoursPerSecond, GameReferences.FpsReadout.FramesPerSecond);
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
        internal bool TryCheckForObstacleEnrouteTo(ApMoveDestinationProxy destProxy, out ApMoveDestinationProxy detourProxy) {
            D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
            int iterationCount = Constants.Zero;
            IAvoidableObstacle unusedObstacleFound;
            bool hasDetour = TryCheckForObstacleEnrouteTo(destProxy, out detourProxy, out unusedObstacleFound, ref iterationCount);
            return hasDetour;
        }
        //internal bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy) {
        //    D.AssertNotNull(destProxy, "{0}.AutoPilotDestProxy is null. Frame = {1}.".Inject(DebugName, Time.frameCount));
        //    int iterationCount = Constants.Zero;
        //    IAvoidableObstacle unusedObstacleFound;
        //    bool hasDetour = TryCheckForObstacleEnrouteTo(destProxy, out detourProxy, out unusedObstacleFound, ref iterationCount);
        //    return hasDetour;
        //}

        private bool TryCheckForObstacleEnrouteTo(ApMoveDestinationProxy destProxy, out ApMoveDestinationProxy detourProxy, out IAvoidableObstacle obstacle, ref int iterationCount) {
            __ValidateIterationCount(iterationCount, destProxy, allowedIterations: 10);
            iterationCount++;
            detourProxy = null;
            obstacle = null;
            Vector3 destBearing = (destProxy.Position - Position).normalized;
            float rayLength = destProxy.ObstacleCheckRayLength;
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
                    _autoPilot.HandleObstacleFoundIsTarget(obstacle);
                }
                else {
                    D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
                        DebugName, obstacle.DebugName, obstacle.Position, destProxy.DebugName, rayLength, obstacleZoneHitDistance);
                    if (TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detourProxy)) {
                        ApMoveDestinationProxy newDetourProxy;
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
        //private bool TryCheckForObstacleEnrouteTo(AutoPilotDestinationProxy destProxy, out AutoPilotDestinationProxy detourProxy, out IAvoidableObstacle obstacle, ref int iterationCount) {
        //    __ValidateIterationCount(iterationCount, destProxy, allowedIterations: 10);
        //    iterationCount++;
        //    detourProxy = null;
        //    obstacle = null;
        //    Vector3 destBearing = (destProxy.Position - Position).normalized;
        //    float rayLength = destProxy.GetObstacleCheckRayLength(Position);
        //    Ray ray = new Ray(Position, destBearing);

        //    bool isDetourGenerated = false;
        //    RaycastHit hitInfo;
        //    if (Physics.Raycast(ray, out hitInfo, rayLength, AvoidableObstacleZoneOnlyLayerMask.value)) {
        //        // there is an AvoidableObstacleZone in the way. Warning: hitInfo.transform returns the rigidbody parent since 
        //        // the obstacleZone trigger collider is static. UNCLEAR if this means it forms a compound collider as this is a raycast
        //        var obstacleZoneGo = hitInfo.collider.gameObject;
        //        var obstacleZoneHitDistance = hitInfo.distance;
        //        obstacle = obstacleZoneGo.GetSafeFirstInterfaceInParents<IAvoidableObstacle>(excludeSelf: true);

        //        if (obstacle == destProxy.Destination) {
        //            D.LogBold(ShowDebugLog, "{0} encountered obstacle {1} which is the destination. \nRay length = {2:0.00}, DistanceToHit = {3:0.00}.",
        //                DebugName, obstacle.DebugName, rayLength, obstacleZoneHitDistance);
        //            _autoPilot.HandleObstacleFoundIsTarget(obstacle);
        //        }
        //        else {
        //            D.Log(ShowDebugLog, "{0} encountered obstacle {1} at {2} when checking approach to {3}. \nRay length = {4:0.#}, DistanceToHit = {5:0.#}.",
        //                DebugName, obstacle.DebugName, obstacle.Position, destProxy.DebugName, rayLength, obstacleZoneHitDistance);
        //            if (TryGenerateDetourAroundObstacle(obstacle, hitInfo, out detourProxy)) {
        //                AutoPilotDestinationProxy newDetourProxy;
        //                IAvoidableObstacle newObstacle;
        //                if (TryCheckForObstacleEnrouteTo(detourProxy, out newDetourProxy, out newObstacle, ref iterationCount)) {
        //                    if (obstacle == newObstacle) {
        //                        // 2.7.17 UNCLEAR redundant? IAvoidableObstacle.GetDetour() should fail if can't get to detour, although check uses math rather than a ray
        //                        D.Error("{0} generated detour {1} that does not get around obstacle {2}.", DebugName, newDetourProxy.DebugName, obstacle.DebugName);
        //                    }
        //                    else {
        //                        D.Log(ShowDebugLog, "{0} found another obstacle {1} on the way to detour {2} around obstacle {3}.", DebugName, newObstacle.DebugName, detourProxy.DebugName, obstacle.DebugName);
        //                    }
        //                    detourProxy = newDetourProxy;
        //                    obstacle = newObstacle; // UNCLEAR whether useful. 2.7.17 Only use is to compare whether obstacle is the same
        //                }
        //                isDetourGenerated = true;
        //            }
        //        }
        //    }
        //    return isDetourGenerated;
        //}

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
        private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out ApMoveDestinationProxy detourProxy) {
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
            float reqdTurnAngleToDetour = Vector3.Angle(_autoPilot.CurrentHeading, detourBearing);
            if (obstacle.IsMobile) {
                if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
                    useDetour = false;
                    // angle is still shallow but short remaining distance might require use of a detour
                    float maxDistanceTraveledBeforeNextObstacleCheck = _autoPilot.IntendedCurrentSpeedValue * _obstacleCheckJobPeriod.TotalInHours;
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
        //private bool TryGenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit zoneHitInfo, out AutoPilotDestinationProxy detourProxy) {
        //    detourProxy = GenerateDetourAroundObstacle(obstacle, zoneHitInfo);
        //    if (MyMath.DoesLineSegmentIntersectSphere(Position, detourProxy.Position, obstacle.Position, obstacle.__ObstacleZoneRadius)) {
        //        // 1.26.17 This can marginally fail when traveling as a fleet when the ship's FleetFormationStation is at the closest edge of the
        //        // formation to the obstacle. As the proxy incorporates this station offset into its "Position" to keep ships from bunching
        //        // up when detouring as a fleet, the resulting detour destination can be very close to the edge of the obstacle's Zone.
        //        // If/when this does occur, I expect the offset to be large.
        //        D.Warn("{0} generated detour {1} that {2} can't get too because {0} is in the way! Offset = {3:0.00}.", obstacle.DebugName, detourProxy.DebugName, DebugName, detourProxy.__DestinationOffset);
        //    }

        //    bool useDetour = true;
        //    Vector3 detourBearing = (detourProxy.Position - Position).normalized;
        //    float reqdTurnAngleToDetour = Vector3.Angle(_autoPilot.CurrentHeading, detourBearing);
        //    if (obstacle.IsMobile) {
        //        if (reqdTurnAngleToDetour < DetourTurnAngleThreshold) {
        //            useDetour = false;
        //            // angle is still shallow but short remaining distance might require use of a detour
        //            float maxDistanceTraveledBeforeNextObstacleCheck = _autoPilot.IntendedCurrentSpeedValue * _obstacleCheckJobPeriod.TotalInHours;
        //            float obstacleDistanceThresholdRequiringDetour = maxDistanceTraveledBeforeNextObstacleCheck * 2F;   // HACK
        //            float distanceToObstacleZone = zoneHitInfo.distance;
        //            if (distanceToObstacleZone <= obstacleDistanceThresholdRequiringDetour) {
        //                useDetour = true;
        //            }
        //        }
        //    }
        //    if (useDetour) {
        //        D.Log(ShowDebugLog, "{0} has generated detour {1} to get by obstacle {2} in Frame {3}. Reqd Turn = {4:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, Time.frameCount, reqdTurnAngleToDetour);
        //    }
        //    else {
        //        D.Log(ShowDebugLog, "{0} has declined to use detour {1} to get by mobile obstacle {2}. Reqd Turn = {3:0.#} degrees.", DebugName, detourProxy.DebugName, obstacle.DebugName, reqdTurnAngleToDetour);
        //    }
        //    return useDetour;
        //}

        /// <summary>
        /// Generates a detour around the provided obstacle. Includes any reqd offset for the
        /// ship when traveling as a fleet.
        /// </summary>
        /// <param name="obstacle">The obstacle.</param>
        /// <param name="hitInfo">The hit information.</param>
        /// <returns></returns>
        private ApMoveDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo) {
            float reqdClearanceRadius = _autoPilot.ReqdObstacleClearanceDistance;
            Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, reqdClearanceRadius);
            StationaryLocation detour = new StationaryLocation(detourPosition);
            Vector3 detourOffset = CalcDetourOffset(detour);
            float tgtStandoffDistance = _autoPilot.ShipCollisionDetectionZoneRadius;
            return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, _autoPilot.Ship);
        }
        //private AutoPilotDestinationProxy GenerateDetourAroundObstacle(IAvoidableObstacle obstacle, RaycastHit hitInfo) {
        //    float reqdClearanceRadius = _autoPilot.ReqdObstacleClearanceDistance;
        //    Vector3 detourPosition = obstacle.GetDetour(Position, hitInfo, reqdClearanceRadius);
        //    StationaryLocation detour = new StationaryLocation(detourPosition);
        //    Vector3 detourOffset = CalcDetourOffset(detour);
        //    float tgtStandoffDistance = _autoPilot.ShipCollisionDetectionZoneRadius;
        //    return detour.GetApMoveTgtProxy(detourOffset, tgtStandoffDistance, Position);
        //}

        private ApMoveDestinationProxy __initialDestination;
        private IList<ApMoveDestinationProxy> __destinationRecord;

        private void __ValidateIterationCount(int iterationCount, ApMoveDestinationProxy destProxy, int allowedIterations) {
            if (iterationCount == Constants.Zero) {
                __initialDestination = destProxy;
            }
            if (iterationCount > Constants.Zero) {
                if (iterationCount == Constants.One) {
                    __destinationRecord = __destinationRecord ?? new List<ApMoveDestinationProxy>(allowedIterations + 1);
                    __destinationRecord.Clear();
                    __destinationRecord.Add(__initialDestination);
                }
                __destinationRecord.Add(destProxy);
                D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
                    .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
            }
        }
        //private AutoPilotDestinationProxy __initialDestination;
        //private IList<AutoPilotDestinationProxy> __destinationRecord;

        //private void __ValidateIterationCount(int iterationCount, AutoPilotDestinationProxy destProxy, int allowedIterations) {
        //    if (iterationCount == Constants.Zero) {
        //        __initialDestination = destProxy;
        //    }
        //    if (iterationCount > Constants.Zero) {
        //        if (iterationCount == Constants.One) {
        //            __destinationRecord = __destinationRecord ?? new List<AutoPilotDestinationProxy>(allowedIterations + 1);
        //            __destinationRecord.Clear();
        //            __destinationRecord.Add(__initialDestination);
        //        }
        //        __destinationRecord.Add(destProxy);
        //        D.AssertException(iterationCount <= allowedIterations, "{0}.ObstacleDetourCheck Iteration Error. Destination & Detours: {1}."
        //            .Inject(DebugName, __destinationRecord.Select(det => det.DebugName).Concatenate()));
        //    }
        //}

        /// <summary>
        /// Calculates and returns the world space offset to the provided detour that when combined with the
        /// detour's position, represents the actual location in world space this ship is trying to reach, 
        /// aka DetourPoint. Used to keep ships from bunching up at the detour when many ships in a fleet encounter the same obstacle.
        /// </summary>
        /// <param name="detour">The detour.</param>
        /// <returns></returns>
        private Vector3 CalcDetourOffset(StationaryLocation detour) {
            if (_autoPilot.IsFleetwideMove) {
                // make separate detour offsets as there may be a lot of ships encountering this detour
                Quaternion shipCurrentRotation = _autoPilot.ShipRotation;
                Vector3 shipToDetourDirection = (detour.Position - Position).normalized;
                Quaternion shipRotationChgReqdToFaceDetour = Quaternion.FromToRotation(_autoPilot.CurrentHeading, shipToDetourDirection);
                Quaternion shipRotationThatFacesDetour = Math3D.AddRotation(shipCurrentRotation, shipRotationChgReqdToFaceDetour);
                Vector3 shipLocalFormationOffset = _autoPilot.ShipLocalFormationOffset;
                Vector3 detourWorldSpaceOffset = Math3D.TransformDirectionMath(shipRotationThatFacesDetour, shipLocalFormationOffset);
                return detourWorldSpaceOffset;
            }
            return Vector3.zero;
        }

        #region Event and Property Change Handlers

        private void OnObstacleFound(ApMoveDestinationProxy detourProxy) {
            if (obstacleFound != null) {
                obstacleFound(this, new ObstacleFoundEventArgs(detourProxy));
            }
        }
        //private void OnObstacleFound(AutoPilotDestinationProxy detourProxy) {
        //    if (obstacleFound != null) {
        //        obstacleFound(this, new ObstacleFoundEventArgs(detourProxy));
        //    }
        //}

        #endregion

        protected override void KillJob() {
            if (_obstacleCheckJob != null) {
                _obstacleCheckJob.Kill();
                _obstacleCheckJob = null;
            }
        }

        public override void ResetForReuse() {
            base.ResetForReuse();
            // obstacleFound is subscribed too only once when this task is created. 
            // This Assert makes sure that hasn't changed.
            D.AssertEqual(1, obstacleFound.GetInvocationList().Count());
            _obstacleCheckJobPeriod = default(GameTimeDuration);
            _doesObstacleCheckPeriodNeedRefresh = false;
        }

        protected override void Cleanup() {
            base.Cleanup();
            obstacleFound = null;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Nested Classes

        public class ObstacleFoundEventArgs : EventArgs {

            public ApMoveDestinationProxy DetourProxy { get; private set; }

            public ObstacleFoundEventArgs(ApMoveDestinationProxy detourProxy) {
                DetourProxy = detourProxy;
            }
            //public AutoPilotDestinationProxy DetourProxy { get; private set; }

            //public ObstacleFoundEventArgs(AutoPilotDestinationProxy detourProxy) {
            //    DetourProxy = detourProxy;
            //}

        }

        #endregion

    }
}

