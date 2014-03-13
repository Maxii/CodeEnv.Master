﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OnStationTracker.cs
// Tracks whether the assigned ship is within the radius of it's Station in the Formation.
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
/// Tracks whether the assigned ship is within the radius of it's Station in the Formation.
/// </summary>
public class OnStationTracker : AMonoBase {
    //public class OnStationTracker : AMonoBase, IDisposable {

    public event Action<bool, Guid> onShipOnStation;

    public Guid ID { get; private set; }

    public float StationRadius { get; private set; }

    private Vector3 _position;
    public Vector3 Position {
        get { return _position; }
        set { SetProperty<Vector3>(ref _position, value, "Position", OnPositionChanged); }
    }

    private ITarget _assignedShip;
    public ITarget AssignedShip {
        get { return _assignedShip; }
        set { SetProperty<ITarget>(ref _assignedShip, value, "AssignedShip", OnAssignedShipChanged, OnAssignedShipChanging); }
    }

    private SphereCollider _collider;
    //private IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        ID = Guid.NewGuid();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        _collider.enabled = false;
        //Subscribe();
    }

    //protected virtual void Subscribe() {
    //    _subscribers = new List<IDisposable>();
    //    _subscribers.Add(GameStatus.Instance.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsRunning, OnIsRunningChanged));
    //}

    void OnTriggerEnter(Collider other) {
        //D.Log("OnTriggerEnter({0}) called.", other.name);
        ITarget target = other.gameObject.GetInterface<ITarget>();
        if (target != null) {
            if (target == AssignedShip) {
                OnShipOnStation(true);
            }
        }
    }

    void OnTriggerExit(Collider other) {
        //D.Log("{0}.OnTriggerExit() called by Collider {1}.", GetType().Name, other.name);
        ITarget target = other.gameObject.GetInterface<ITarget>();
        if (target != null) {
            if (target == AssignedShip) {
                OnShipOnStation(false);
            }
        }
    }

    //private void OnIsRunningChanged() {
    //    if (GameStatus.Instance.IsRunning) {
    //        _collider.enabled = true;
    //    }
    //}

    private void OnAssignedShipChanging(ITarget newShip) {
        if (AssignedShip != null) {
            AssignedShip.onItemDeath -= OnAssignedShipDeath;
        }
    }

    private void OnAssignedShipChanged() {
        if (AssignedShip != null) {
            StationRadius = AssignedShip.Radius * 5F;
            _collider.radius = StationRadius;
            AssignedShip.onItemDeath += OnAssignedShipDeath;
            _collider.enabled = true;
            if (IsShipAlreadyOnStation) {   // in case ship is already present inside collider
                OnShipOnStation(true);
            }
        }
        else {
            _collider.enabled = false;
        }
    }

    private void OnAssignedShipDeath(ITarget deadAssignedShip) {
        OnShipOnStation(false);
        AssignedShip = null;
    }

    private void OnPositionChanged() {
        _transform.position = Position;
    }

    protected void OnShipOnStation(bool isOnStation) {
        var temp = onShipOnStation;
        if (temp != null) {
            temp(isOnStation, ID);
        }
    }

    private bool IsShipAlreadyOnStation {
        get { return _collider.bounds.Contains(AssignedShip.Position); }
    }

    //protected override void OnDestroy() {
    //    base.OnDestroy();
    //    Dispose();
    //}

    //private void Cleanup() {
    //    Unsubscribe();
    //    // other cleanup here including any tracking Gui2D elements
    //}

    //private void Unsubscribe() {
    //    _subscribers.ForAll(d => d.Dispose());
    //    _subscribers.Clear();
    //}

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region IDisposable
    //[DoNotSerialize]
    //private bool _alreadyDisposed = false;
    //protected bool _isDisposing = false;

    ///// <summary>
    ///// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    ///// </summary>
    //public void Dispose() {
    //    Dispose(true);
    //    GC.SuppressFinalize(this);
    //}

    ///// <summary>
    ///// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    ///// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    ///// </summary>
    ///// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    //protected virtual void Dispose(bool isDisposing) {
    //    // Allows Dispose(isDisposing) to be called more than once
    //    if (_alreadyDisposed) {
    //        return;
    //    }

    //    _isDisposing = true;
    //    if (isDisposing) {
    //        // free managed resources here including unhooking events
    //        Cleanup();
    //    }
    //    // free unmanaged resources here

    //    _alreadyDisposed = true;
    //}

    //// Example method showing check for whether the object has been disposed
    ////public void ExampleMethod() {
    ////    // throw Exception if called on object that is already disposed
    ////    if(alreadyDisposed) {
    ////        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    ////    }

    ////    // method content here
    ////}
    //#endregion

}

