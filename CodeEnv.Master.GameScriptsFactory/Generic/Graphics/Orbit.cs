﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Orbit.cs
// Class that simulates the movement of an object orbiting around a stationary location. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of an object orbiting around a stationary
/// location. Both orbital movement and self rotation of the orbiting object are implemented.
/// Assumes this script is attached to the parent of an orbiting object. The position  of this
/// parent should be coincident with that of the stationary object the orbiting object is orbiting. 
/// This script simulates orbital movement of the orbiting object by rotating this parent object.
/// </summary>
public class Orbit : AMonoBase, IDisposable {

    /// <summary>
    /// The axis of orbit in local space.
    /// </summary>
    public Vector3 axisOfOrbit = Vector3.up;

    /// <summary>
    /// The relative orbit speed of the object around the location. A value of 1 means
    /// an orbit will take one Year.
    /// </summary>
    public float relativeOrbitSpeed = 1.0F;

    /// <summary>
    /// The duration of one orbit of the object around the location.
    /// </summary>
    private GameTimeDuration _orbitPeriod; // IMPROVE use custom editor to make setable from inspector

    /// <summary>
    /// The orbit speed of the object around the stationary location in degrees per second.
    /// </summary>
    protected float _orbitSpeed;

    private GameStatus _gameStatus;

    protected override void Awake() {
        base.Awake();
        _gameStatus = GameStatus.Instance;
        _orbitPeriod = GameTimeDuration.OneYear;
        _orbitSpeed = relativeOrbitSpeed * Constants.DegreesPerOrbit * (GameTime.HoursPerSecond / (float)_orbitPeriod.TotalInHours);
        UpdateRate = FrameUpdateFrequency.Frequent;
        if (!GameStatus.Instance.IsRunning) {
            GameStatus.Instance.onIsRunning_OneShot += OnGameIsRunning;
            enabled = false;
        }
    }

    private void OnGameIsRunning() {
        enabled = true;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTime = GameTime.DeltaTimeOrPausedWithGameSpeed * (int)UpdateRate;    // stops the orbit when paused
        if (!_gameStatus.IsPaused) {
            UpdateOrbit(deltaTime);
        }
    }

    /// <summary>
    /// Updates the rotation of this object around its local Y axis (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    protected virtual void UpdateOrbit(float deltaTime) {
        _transform.Rotate(axisOfOrbit * _orbitSpeed * deltaTime, relativeTo: Space.Self);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        // This gameObject's parent(s) can be destroyed before the game isRunning.
        // Examples include 1) SettlementCreators when there are more 
        // settlements than systems to assign them too, 2) Planets when there are more
        // planets than orbit slots, etc.
        GameStatus.Instance.onIsRunning_OneShot -= OnGameIsRunning;
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

