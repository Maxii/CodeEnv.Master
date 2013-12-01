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
        private float _separationTestToleranceDistanceSqrd;

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
        /// Checks whether the pilot can approach the Destination directly.
        /// </summary>
        /// <returns><c>true</c> if there is nothing obstructing a direct approach.</returns>
        protected virtual bool CheckDirectApproachToDestination() {
            Vector3 currentPosition = Data.Position;
            Vector3 directionToDestination = (Destination - currentPosition).normalized;
            float rayDistance = Vector3.Distance(currentPosition, Destination) - Target.Radius - _closeEnoughDistance;
            float clampedRayDistance = Mathf.Clamp(rayDistance, Constants.ZeroF, Mathf.Infinity);
            RaycastHit hitInfo;
            if (Physics.Raycast(currentPosition, directionToDestination, out hitInfo, clampedRayDistance)) {
                D.Warn("{0} obstacle encountered when checking approach to Destination.", hitInfo.collider.name);
                D.Warn("RayDistance = {0}, TargetRadius = {1:0.00}, CloseEnoughDistance = {2}.", rayDistance, Target.Radius, _closeEnoughDistance);
                // there is an obstacle in the way so continue to follow the course
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks whether the distance between 2 objects is increasing.
        /// </summary>
        /// <param name="distanceToCurrentDestinationSqrd">The distance automatic current destination SQRD.</param>
        /// <param name="previousDistanceSqrd">The previous distance SQRD.</param>
        /// <returns>true if the separation distance is increasing.</returns>
        protected bool CheckSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
            if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + _separationTestToleranceDistanceSqrd) {
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
            _separationTestToleranceDistanceSqrd = speedFactor * speedFactor;
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

