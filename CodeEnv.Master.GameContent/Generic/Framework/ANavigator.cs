// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ANavigator.cs
// Abstract base class for Item navigators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Item navigators.
    /// </summary>
    public abstract class ANavigator : APropertyChangeTracking, IDisposable {

        /// <summary>
        /// The ITarget this navigator is trying to reach. Can simply be a location.
        /// </summary>
        public ITarget Target { get; private set; }

        /// <summary>
        /// From orders, the speed to travel at.
        /// </summary>
        public float Speed { get; private set; }

        /// <summary>
        /// The world space location of the target.
        /// </summary>
        protected abstract Vector3 Destination { get; }

        public bool IsEngaged {
            get { return _pilotJob != null && _pilotJob.IsRunning; }
        }

        protected Data Data { get; private set; }

        protected static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

        /// <summary>
        /// The duration in seconds between course updates. The default is
        /// every second at normal gamespeed.
        /// </summary>
        protected float _courseUpdatePeriod = 1F;
        protected float _closeEnoughDistance;
        protected float _closeEnoughDistanceSqrd;

        /// <summary>
        /// The tolerance value used to test whether separation between 2 items is increasing. This 
        /// is a squared value.
        /// </summary>
        private float __separationTestToleranceDistanceSqrd;

        protected IList<IDisposable> _subscribers;
        private GameTime _gameTime;
        protected float _gameSpeedMultiplier;
        protected Job _pilotJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="ANavigator" /> class.
        /// </summary>
        /// <param name="data">Item data.</param>
        public ANavigator(Data data) {
            Data = data;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _courseUpdatePeriod /= _gameSpeedMultiplier;
            // Subscribe called by derived classes so all constructor references can be initialized before they are used by Subscribe
        }

        protected virtual void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        protected virtual void OnDestinationReached() {
            D.Log("{0} has reached Destination {1}. Actual proximity {2} units.", Data.Name, Target.Name, Vector3.Distance(Destination, Data.Position));
            _pilotJob.Kill();
        }

        protected virtual void OnCourseTrackingError() {
            _pilotJob.Kill();
        }

        private void OnGameSpeedChanged() {
            float previousGameSpeedMultiplier = _gameSpeedMultiplier;   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            float gameSpeedChangeRatio = _gameSpeedMultiplier / previousGameSpeedMultiplier;
            AdjustForGameSpeed(gameSpeedChangeRatio);
        }

        /// <summary>
        /// Plots a course and notifies the requester of the outcome via the onCoursePlotCompleted event if set.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        public virtual void PlotCourse(ITarget target, float speed) {
            Target = target;
            Speed = speed;
            InitializeTargetValues();
        }

        /// <summary>
        /// Engages pilot execution to Destination either by direct
        /// approach or following a course.
        /// </summary>
        public virtual void Engage() {
            if (_pilotJob != null && _pilotJob.IsRunning) {
                _pilotJob.Kill();
            }
        }

        /// <summary>
        /// Primary external control to disengage the pilot once Engage has been called.
        /// </summary>
        public virtual void Disengage() {
            if (IsEngaged) {
                _pilotJob.Kill();
            }
        }

        /// <summary>
        /// Checks whether the pilot can approach the provided location directly.
        /// </summary>
        /// <param name="location">The location to approach.</param>
        /// <returns>
        ///   <c>true</c> if there is nothing obstructing a direct approach.
        /// </returns>
        protected virtual bool CheckApproachTo(Vector3 location) {
            Vector3 currentPosition = Data.Position;
            Vector3 vectorToLocation = location - currentPosition;
            float distanceToLocation = vectorToLocation.magnitude;
            Vector3 directionToLocation = vectorToLocation.normalized;
            float rayDistance = distanceToLocation - _closeEnoughDistance;
            float clampedRayDistance = Mathf.Clamp(rayDistance, Constants.ZeroF, Mathf.Infinity);
            RaycastHit hitInfo;
            if (Physics.Raycast(currentPosition, directionToLocation, out hitInfo, clampedRayDistance, _keepoutOnlyLayerMask.value)) {
                D.Warn("{0} encountered obstacle {1} when checking approach to {2}.", Data.Name, hitInfo.collider.name, location);
                // there is a keepout zone obstacle in the way 
                return false;
            }
            return true;
        }

        /// <summary>
        /// Finds the obstacle in the way of approaching location and develops and
        /// returns a waypoint location that will avoid it.
        /// </summary>
        /// <param name="location">The location we are trying to reach that has an obstacle in the way.</param>
        /// <returns>A waypoint location that will avoid the obstacle.</returns>
        protected Vector3 GetWaypointAroundObstacleTo(Vector3 location) {
            Vector3 currentPosition = Data.Position;
            Vector3 vectorToLocation = location - currentPosition;
            float distanceToLocation = vectorToLocation.magnitude;
            Vector3 directionToLocation = vectorToLocation.normalized;

            Vector3 waypoint = Vector3.zero;

            Ray ray = new Ray(currentPosition, directionToLocation);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo, distanceToLocation, _keepoutOnlyLayerMask.value)) {
                // found a keepout zone, so find the point on the other side of the zone where the ray came out
                Vector3 rayEntryPoint = hitInfo.point;
                float keepoutRadius = (hitInfo.collider as SphereCollider).radius;
                float maxKeepoutDiameter = TempGameValues.StarKeepoutRadius * 2F;
                Vector3 pointBeyondKeepoutZone = ray.GetPoint(hitInfo.distance + maxKeepoutDiameter);
                if (Physics.Raycast(pointBeyondKeepoutZone, -ray.direction, out hitInfo, maxKeepoutDiameter, _keepoutOnlyLayerMask.value)) {
                    Vector3 rayExitPoint = hitInfo.point;
                    Vector3 halfWayPointInsideKeepoutZone = rayEntryPoint + (rayExitPoint - rayEntryPoint) / 2F;
                    Vector3 obstacleCenter = hitInfo.collider.transform.position;
                    waypoint = obstacleCenter + (halfWayPointInsideKeepoutZone - obstacleCenter).normalized * (keepoutRadius + _closeEnoughDistance);
                    D.Log("{0}'s waypoint to avoid obstacle = {1}.", Data.Name, waypoint);
                }
                else {
                    D.Error("{0} did not find a ray exit point when casting through {1}.", Data.Name, hitInfo.collider.name);
                }
            }
            else {
                D.Error("{0} did not find an obstacle.", Data.Name);
            }
            return waypoint;
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

        /// <summary>
        /// Initializes the values that depend on the target and speed.
        /// </summary>
        /// <returns>SpeedFactor, a multiple of Speed used in the calculations. Simply a convenience for derived classes.</returns>
        protected virtual float InitializeTargetValues() {
            float speedFactor = Speed * 3F;
            __separationTestToleranceDistanceSqrd = speedFactor * speedFactor;   // FIXME needs work - courseUpdatePeriod???
            _closeEnoughDistance = Target.Radius + speedFactor;  // IMPROVE range should be a factor too
            _closeEnoughDistanceSqrd = _closeEnoughDistance * _closeEnoughDistance;
            return speedFactor;
        }

        /// <summary>
        /// Adjusts various factors to reflect the new GameClockSpeed setting. 
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        protected virtual void AdjustForGameSpeed(float gameSpeedChangeRatio) {
            _courseUpdatePeriod /= gameSpeedChangeRatio;
        }

        protected virtual void Cleanup() {
            Unsubscribe();
            if (_pilotJob != null) {
                _pilotJob.Kill();
            }
        }

        private void Unsubscribe() {
            _subscribers.ForAll<IDisposable>(s => s.Dispose());
            _subscribers.Clear();
            // subscriptions contained completely within this gameobject (both subscriber
            // and subscribee) donot have to be cleaned up as all instances are destroyed
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

    }
}

