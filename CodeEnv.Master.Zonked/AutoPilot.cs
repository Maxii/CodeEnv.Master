// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AutoPilot.cs
// AutoPilot for Ships with no pathfinding capability.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AutoPilot for Ships with no pathfinding capability.
/// </summary>
public class AutoPilot : APropertyChangeTracking, IDisposable {

    /// <summary>
    /// Optional delegate for notification of the destination being reached.
    /// </summary>
    public Action onDestinationReached;

    private Vector3 _destination;
    /// <summary>
    /// Readonly. The destination of this autopilot.
    /// </summary>
    public Vector3 Destination {
        get { return _destination; }
        private set { SetProperty<Vector3>(ref _destination, value, "Destination"); }
    }

    public bool IsEngaged {
        get { return _job != null && _job.IsRunning; }
    }

    private float _closeEnoughToWaypointDistanceSqrd = 25F;
    private float _courseUpdateFrequency = 1F;
    private int _courseLegCheckFrequency = 5;

    private Job _job;

    private ShipModel _ship;
    private ShipData _shipData;
    private IList<IDisposable> _subscribers;

    public AutoPilot(ShipModel ship) {
        _ship = ship;
        _shipData = ship.Data;
        _courseUpdateFrequency /= GameTime.Instance.GameSpeed.SpeedMultiplier();
        Subscribe();
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(GameTime.Instance.SubscribeToPropertyChanging<GameTime, GameClockSpeed>(gt => gt.GameSpeed, OnGameSpeedChanging));
        onDestinationReached += OnDestinationReached;
    }

    /// <summary>
    /// Engages autopilot execution to FinalDestination either by direct
    /// approach or following a course.
    /// </summary>
    public void Engage(Vector3 destination) {
        if (_job != null && _job.IsRunning) {
            _job.Kill();
        }
        Destination = destination;
        _job = new Job(Engage(), true);
    }

    /// <summary>
    /// Primary external control to disengage the autopilot once Engage has been called.
    /// </summary>
    public void Disengage() {
        if (IsEngaged) {
            _job.Kill();
        }
    }

    /// <summary>
    /// Engages autopilot execution of a direct path to the final Destination. No A* course is used.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Engage() {
        D.Log("Initiating coroutine for approach to {0}.", Destination);

        Vector3 newHeading = (Destination - _shipData.Position).normalized;
        AdjustHeadingAndSpeedForTurn(newHeading);

        float initialDistanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _shipData.Position);
        int checksRemaining = _courseLegCheckFrequency - 1;

        bool isSpeedIncreaseMade = false;

        float distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _shipData.Position);
        while (distanceToDestinationSqrd > _closeEnoughToWaypointDistanceSqrd) {
            //D.Log("Distance to Destination = {0}.", distanceToDestination);
            if (!isSpeedIncreaseMade) {    // adjusts speed as a oneshot until we get there
                isSpeedIncreaseMade = IncreaseSpeedOnHeadingConfirmation();
            }
            CheckCourse(Destination, initialDistanceToDestinationSqrd, ref checksRemaining);
            distanceToDestinationSqrd = Vector3.SqrMagnitude(Destination - _shipData.Position);
            yield return new WaitForSeconds(_courseUpdateFrequency);
        }
        //D.Log("Final Approach coroutine ended.");
        onDestinationReached();
    }

    private void OnGameSpeedChanging(GameClockSpeed newGameSpeed) {
        _courseUpdateFrequency *= GameTime.Instance.GameSpeed.SpeedMultiplier() / newGameSpeed.SpeedMultiplier();
    }

    private void OnDestinationReached() {
        _job.Kill();
    }

    private void AdjustHeadingAndSpeedForTurn(Vector3 newHeading) {
        _ship.ChangeSpeed(0.1F, isAutoPilot: false); // slow for the turn
        _ship.ChangeHeading(newHeading, isAutoPilot: false);
    }

    /// <summary>
    /// Increases the speed of the fleet when the correct heading has been achieved.
    /// </summary>
    /// <returns><c>true</c> if the heading is confirmed and speed changed.</returns>
    private bool IncreaseSpeedOnHeadingConfirmation() {
        if (CodeEnv.Master.Common.Mathfx.Approx(_shipData.CurrentHeading, _shipData.RequestedHeading, .1F)) {
            // we are close to being on course, so punch it up to warp 9!
            _ship.ChangeSpeed(2.0F, isAutoPilot: false);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks the course and makes any heading corrections needed.
    /// </summary>
    /// <param name="destination">The current destination.</param>
    /// <param name="distanceBetweenDestinationsSqrd">The distance between destinations SQRD.</param>
    /// <param name="checksRemaining">The number of course checks remaining on this leg.</param>
    private void CheckCourse(Vector3 destination, float distanceBetweenDestinationsSqrd, ref int checksRemaining) {
        if (checksRemaining <= 0) {
            return;
        }
        float distanceToDestinationSqrd = Vector3.SqrMagnitude(destination - _shipData.Position);
        float nextCheckDistanceSqrd = distanceBetweenDestinationsSqrd * (checksRemaining / (float)_courseLegCheckFrequency);
        if (distanceToDestinationSqrd < nextCheckDistanceSqrd) {
            Vector3 newHeading = (destination - _shipData.Position).normalized;
            if (!CodeEnv.Master.Common.Mathfx.Approx(newHeading, _shipData.RequestedHeading, .01F)) {
                //_fleet.ChangeFleetSpeed(0.1F, isManualOverride: false);
                _ship.ChangeHeading(newHeading, isAutoPilot: false);
                D.Log("{0} has made a midcourse correction to {1} at checkpoint {2} of {3}.",
                    _ship.Data.Name, newHeading, checksRemaining, _courseLegCheckFrequency);
            }
            else {
                D.Log("{0} has made a midcourse correction check with no change at checkpoint {1} of {2}.",
                    _ship.Data.Name, checksRemaining, _courseLegCheckFrequency);
            }
            checksRemaining--;
        }
    }

    private void Cleanup() {
        Unsubscribe();
        if (_job != null) {
            _job.Kill();
        }
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
        // subscriptions contained completely within this gameobject (both subscriber
        // and subscribee) donot have to be cleaned up as all instances are destroyed
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
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

