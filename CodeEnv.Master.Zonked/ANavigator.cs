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
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Item navigators.
    /// </summary>
    public abstract class ANavigator : APropertyChangeTracking, IDisposable {

        private Vector3 _destination;
        /// <summary>
        /// Readonly. The destination of the item this navigator belongs too. If a fleet, it is the
        /// fleet's final destination, not a waypoint. If a ship, it is the ship's current destination,
        /// many times being a waypoint of the fleet it is part of, 
        /// adjusted for the ship's relative local position in the fleet's formation.
        /// </summary>
        public Vector3 Destination {
            get { return _destination; }
            protected set { SetProperty<Vector3>(ref _destination, value, "Destination"); }
        }

        public bool IsEngaged {
            get { return _pilotJob != null && _pilotJob.IsRunning; }
        }

        /// <summary>
        /// The duration in seconds between course updates. The default is
        /// every second at normal gamespeed.
        /// </summary>
        protected float _courseUpdatePeriod = 1F;
        protected float _closeEnoughDistanceSqrd = 25F;
        //private float _courseTrackingErrorDistanceToleranceSqrd = 25F;

        protected IList<IDisposable> _subscribers;

        private GameTime _gameTime;
        protected float _gameSpeedMultiplier;

        protected Job _pilotJob;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipNavigator"/> class.
        /// </summary>
        /// <param name="t">Ship Transform</param>
        /// <param name="data">Ship data.</param>
        public ANavigator() {
            _gameTime = GameTime.Instance;
            _gameSpeedMultiplier = _gameTime.GameSpeed.SpeedMultiplier();   // FIXME where/when to get initial GameSpeed before first GameSpeed change?
            _courseProgressAssessmentPeriod /= _gameSpeedMultiplier;
            // Subscribe called by derived classes so all constructor references can be initialized before they are used by Subscribe
        }

        protected virtual void Subscribe() {
            _subscribers = new List<IDisposable>();
            _subscribers.Add(_gameTime.SubscribeToPropertyChanged<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanged));
        }

        protected virtual void OnDestinationReached() {
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
        /// <param name="destination">The destination.</param>
        public abstract void PlotCourse(Vector3 destination);

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
        public void Disengage() {
            if (IsEngaged) {
                _pilotJob.Kill();
            }
        }

        //[Obsolete] // replaced by GameUtility.CheckForIncreasingSeparation
        //protected bool CheckForCourseTrackingError(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
        //    if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + _courseTrackingErrorDistanceToleranceSqrd) {
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
        /// Adjusts various factors to reflect the new GameClockSpeed setting. 
        /// </summary>
        /// <param name="gameSpeed">The game speed.</param>
        protected virtual void AdjustForGameSpeed(float gameSpeedChangeRatio) {
            _courseProgressAssessmentPeriod /= gameSpeedChangeRatio;
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

