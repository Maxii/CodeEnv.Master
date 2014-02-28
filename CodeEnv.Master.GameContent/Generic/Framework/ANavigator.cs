﻿// --------------------------------------------------------------------------------------------------------------------
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
        public event Action onCourseTrackingError;

        /// <summary>
        /// The IDestinationTarget this navigator is trying to reach. Can simply be a location.
        /// </summary>
        public IDestinationTarget Target { get; private set; }

        private float _speed;
        /// <summary>
        /// The speed to travel at.
        /// </summary>
        public float Speed {
            get { return _speed; }
            set { SetProperty<float>(ref _speed, value, "Speed", OnSpeedChanged); }
        }

        /// <summary>
        /// The world space location of the target.
        /// </summary>
        protected virtual Vector3 Destination { get { return Target.Position; } }

        public bool IsEngaged {
            get { return _pilotJob != null && _pilotJob.IsRunning; }
        }

        private float _desiredDistanceFromTarget;
        protected float DesiredDistanceFromTarget {
            get { return _desiredDistanceFromTarget; }
            set {
                _desiredDistanceFromTarget = value;
                _desiredDistanceFromTargetSqrd = _desiredDistanceFromTarget * _desiredDistanceFromTarget;
            }
        }

        protected AMortalItemData Data { get; private set; }

        protected static LayerMask _keepoutOnlyLayerMask = LayerMaskExtensions.CreateInclusiveMask(Layers.CelestialObjectKeepout);

        /// <summary>
        /// The duration in seconds between course progress assessments. The default is
        /// every second at a speed of 1 unit per day and normal gamespeed.
        /// </summary>
        protected float _courseProgressCheckPeriod = 1F;
        protected float _desiredDistanceFromTargetSqrd;

        protected IList<IDisposable> _subscribers;
        private GameTime _gameTime;
        protected float _gameSpeedMultiplier;
        protected Job _pilotJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="ANavigator" /> class.
        /// </summary>
        /// <param name="data">Item data.</param>
        public ANavigator(AMortalItemData data) {
            Data = data;
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _courseProgressCheckPeriod /= _gameSpeedMultiplier;    // speed not set yet
            // Subscribe called by derived classes so all constructor references can be initialized before they are used by Subscribe
        }

        protected virtual void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        protected void OnCoursePlotFailure() {
            var temp = onCoursePlotFailure;
            if (temp != null) {
                temp();
            }
        }

        protected void OnCoursePlotSuccess() {
            var temp = onCoursePlotSuccess;
            if (temp != null) {
                temp();
            }
        }

        protected virtual void OnDestinationReached() {
            _pilotJob.Kill();
            D.Log("{0} has reached Destination {1}. Actual proximity {2} units.", Data.Name, Target.Name, Vector3.Distance(Destination, Data.Position));
            var temp = onDestinationReached;
            if (temp != null) {
                temp();
            }
        }

        protected virtual void OnCourseTrackingError() {
            _pilotJob.Kill();
            var temp = onCourseTrackingError;
            if (temp != null) {
                temp();
            }
        }

        protected void OnWeaponsRangeChanged() {
            InitializeTargetValues();
        }

        private void OnSpeedChanged() {
            AssessFrequencyOfCourseProgressChecks();
        }

        private void OnGameSpeedChanged() {
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();
            AssessFrequencyOfCourseProgressChecks();
        }

        /// <summary>
        /// Plots a course and notifies the requester of the outcome via the onCoursePlotCompleted event if set.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="speed">The speed.</param>
        public virtual void PlotCourse(IDestinationTarget target, float speed) {
            Target = target;
            Speed = speed;
            D.Assert(speed != Constants.ZeroF, "Designated speed to new target {0} is 0!".Inject(target.Name));
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
                D.Log("{0} Navigator disengaging.", Data.Name);
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
            if (distanceToLocation < DesiredDistanceFromTarget) {
                // already inside close enough distance
                return true;
            }
            Vector3 directionToLocation = vectorToLocation.normalized;
            float rayDistance = distanceToLocation - DesiredDistanceFromTarget;
            float clampedRayDistance = Mathf.Clamp(rayDistance, 0.1F, Mathf.Infinity);
            RaycastHit hitInfo;
            if (Physics.Raycast(currentPosition, directionToLocation, out hitInfo, clampedRayDistance, _keepoutOnlyLayerMask.value)) {
                D.Warn("{0} encountered obstacle {1} when checking approach to {2}.", Data.Name, hitInfo.collider.name, location);
                // there is a keepout zone obstacle in the way 
                return false;
            }
            return true;
        }

        protected abstract void InitializeTargetValues();

        private void AssessFrequencyOfCourseProgressChecks() {
            // frequency of course progress checks increases as speed and gameSpeed increase
            float courseProgressCheckFrequency = 1F + (Speed * _gameSpeedMultiplier);
            _courseProgressCheckPeriod = 1F / courseProgressCheckFrequency;
            D.Log("{0}.{1} frequency of course progress checks adjusted to {2:0.####}.", Data.Name, GetType().Name, courseProgressCheckFrequency);
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

            _isDisposing = true;
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
}

