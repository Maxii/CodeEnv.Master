// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Revolve.cs
// Class that rotates an object around a designated axis.
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
/// Class that rotates an object around a designated axis.
/// </summary>
public class Revolve : AMonoBase, IDisposable {

    /// <summary>
    /// The axis of self rotation in local space.
    /// </summary>
    public Vector3 axisOfRotation = Vector3.up;

    /// <summary>
    /// The self rotation speed of the object on its own axis. A value of 1 means
    /// a single rotation will take one Day.
    /// </summary>
    public float relativeRotationSpeed = 0.1F;

    /// <summary>
    /// The duration of one rotation of the object on its own axis.
    /// </summary>
    public GameTimePeriod rotationPeriod;

    /// <summary>
    /// The self rotation speed of the object around its own axis in degrees per second.
    /// </summary>
    private float _rotationSpeed;

    protected override void Awake() {
        base.Awake();
        UnityUtility.ValidateComponentPresence<MeshRenderer>(gameObject);
        rotationPeriod = rotationPeriod ?? new GameTimePeriod(days: 1, years: 0);

        _rotationSpeed = (relativeRotationSpeed * Constants.DegreesPerRotation *
            (GameDate.HoursPerSecond / GameDate.HoursPerDay)) / rotationPeriod.PeriodInDays;

        UpdateRate = FrameUpdateFrequency.Frequent;
        if (!GameStatus.Instance.IsRunning) {
            GameStatus.Instance.onIsRunning_OneShot += OnGameIsRunning;
            enabled = false;
        }
    }

    private void OnGameIsRunning() {
        enabled = true;
    }

    void OnBecameVisible() {
        enabled = true;
        //D.Log("{0}.enabled.", _transform.name);
    }

    void OnBecameInvisible() {
        enabled = false;
        //D.Log("{0}.disabled.", _transform.name);
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        float deltaTime = GameTime.DeltaTimeWithGameSpeed * (int)UpdateRate;    // Rotates when paused
        UpdateRotation(deltaTime);
    }

    /// <summary>
    /// Updates the rotation of 'rotatingObject' around its own local Y axis.
    /// </summary>
    /// <param name="deltaTime">The delta time.</param>
    private void UpdateRotation(float deltaTime) {
        _transform.Rotate(axisOfRotation * _rotationSpeed * deltaTime, relativeTo: Space.Self);
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
        // even though the OneShot will unsubscribe this object once Raised, this object can be destroyed
        // prior to the game starting (when an extra planet is destroyed by SystemCreator) so we need
        // to unsubscribe in case destruction occurs before the game starts running
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

        _isDisposing = isDisposing;
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

