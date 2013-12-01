// --------------------------------------------------------------------------------------------------------------------
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

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Class that simulates the movement of an object orbiting around a stationary
/// location. Both orbital movement and self rotation of the orbiting object are implemented.
/// Assumes this script is attached to a parent of [rotatingObject] whose position is coincident
/// with that of the stationary object it is orbiting. This script simulates
/// orbital movement of [rotatingObject] by rotating this parent object.
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
    public GameTimePeriod orbitPeriod;

    /// <summary>
    /// The axis of self rotation in local space.
    /// </summary>
    public Vector3 axisOfRotation = Vector3.up;

    /// <summary>
    /// The self rotation speed of the object on its own axis. A value of 1 means
    /// a single rotation will take one Day.
    /// </summary>
    public float relativeRotationSpeed = 1.0F;

    /// <summary>
    /// The duration of one rotation of the object on its own axis.
    /// </summary>
    public GameTimePeriod rotationPeriod;

    /// <summary>
    /// The object that is to self rotate at [rotationSpeed]. Can be null if the object is not
    /// to self rotate.
    /// </summary>
    public Transform rotatingObject;

    /// <summary>
    /// The orbit speed of the object around the stationary location in degrees per second.
    /// </summary>
    protected float _orbitSpeed;

    /// <summary>
    /// The self rotation speed of the object around its own axis in degrees per second.
    /// </summary>
    protected float _rotationSpeed;

    protected float _gameSpeedMultiplier;
    private GameStatus _gameStatus;

    private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        _gameStatus = GameStatus.Instance;
        orbitPeriod = orbitPeriod ?? new GameTimePeriod(days: 0, years: 1);
        rotationPeriod = rotationPeriod ?? new GameTimePeriod(days: 10, years: 0);
        _orbitSpeed = (relativeOrbitSpeed * Constants.DegreesPerOrbit * GeneralSettings.Instance.DaysPerSecond) / orbitPeriod.PeriodInDays;
        _rotationSpeed = (relativeRotationSpeed * Constants.DegreesPerRotation * GeneralSettings.Instance.DaysPerSecond) / rotationPeriod.PeriodInDays;
        Subscribe();
        UpdateRate = FrameUpdateFrequency.Frequent;
        enabled = false;
    }

    private void Subscribe() {
        if (_subscribers == null) {
            _subscribers = new List<IDisposable>();
        }
        _subscribers.Add(_gameStatus.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsRunning, OnIsRunningChanged));
    }

    private void OnIsRunningChanged() {
        enabled = GameStatus.Instance.IsRunning;
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTime = GameTime.DeltaTimeOrPausedWithGameSpeed * (int)UpdateRate;
        if (!_gameStatus.IsPaused) {
            // we want the rotation of the object to continue as it is just eye candy, but not its position within the system
            UpdateOrbit(deltaTime);
        }
        UpdateRotation(deltaTime);
    }

    /// <summary>
    /// Updates the rotation of this object around its local Y axis (it is coincident with the position of the object being orbited)
    /// to simulate the orbit of this object's child around the object orbited.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    protected virtual void UpdateOrbit(float deltaTime) {
        _transform.Rotate(axisOfOrbit * _orbitSpeed * deltaTime, relativeTo: Space.Self);
    }

    /// <summary>
    /// Updates the rotation of 'rotatingObject' around its own local Y axis.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    private void UpdateRotation(float deltaTime) {
        if (rotatingObject != null) {
            rotatingObject.Rotate(axisOfRotation * _rotationSpeed * deltaTime, relativeTo: Space.Self);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    private void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
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

