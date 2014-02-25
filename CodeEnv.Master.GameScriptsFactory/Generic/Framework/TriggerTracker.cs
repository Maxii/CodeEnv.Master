﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TriggerTracker.cs
// Maintains a list of all ITargets present inside the trigger collider this script is attached too.
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
/// Maintains a list of all ITargets present inside the trigger collider this script is attached too.
/// </summary>
public class TriggerTracker : AMonoBase, IDisposable {

    /// <summary>
    /// Flag indicating whether other Colliders that are triggers are to be tracked.
    /// </summary>
    public bool trackOtherTriggers;

    private static IList<Collider> _collidersToIgnore = new List<Collider>();

    public IList<ITarget> AllTargets { get; private set; }

    protected Collider Collider { get; private set; }

    protected IList<IDisposable> _subscribers;
    protected bool _isInitialized;

    protected override void Awake() {
        base.Awake();
        Collider = UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        Collider.enabled = false;
        Collider.isTrigger = true;
        AllTargets = new List<ITarget>();
        Subscribe();
    }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(GameStatus.Instance.SubscribeToPropertyChanged<GameStatus, bool>(gs => gs.IsRunning, OnIsRunningChanged));
    }

    private void OnIsRunningChanged() {
        if (GameStatus.Instance.IsRunning) {
            _isInitialized = true;
            Collider.enabled = true;
        }
    }

    void OnTriggerEnter(Collider other) {
        //D.Log("OnTriggerEnter({0}) called.", other.name);
        if (!trackOtherTriggers && other.isTrigger) {
            //D.Warn("Ignored Trigger Collider {0}.", other.name);
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        ITarget target = other.gameObject.GetInterface<ITarget>();
        if (target == null) {
            _collidersToIgnore.Add(other);
            D.Warn("Now ignoring Collider {0}.", other.name);
            return;
        }

        Add(target);
    }

    void OnTriggerExit(Collider other) {
        //D.Log("{0}.OnTriggerExit() called by Collider {1}.", GetType().Name, other.name);
        if (!trackOtherTriggers && other.isTrigger) {
            return;
        }

        if (_collidersToIgnore.Contains(other)) {
            return;
        }

        ITarget target = other.gameObject.GetInterface<ITarget>();
        if (target != null) {
            Remove(target);
        }
    }

    protected virtual void Add(ITarget target) {
        if (!AllTargets.Contains(target)) {
            if (!target.IsDead) {
                D.Log("Now tracking target {0}.", target.Name);
                target.onItemDeath += OnTargetDeath;
                target.onOwnerChanged += OnTargetOwnerChanged;
                AllTargets.Add(target);
            }
            else {
                D.Warn("Avoided adding target {0} that is already dead but not yet destroyed.", target.Name);
            }
        }
        else {
            D.Warn("Attempted to add duplicate Target {0}.", target.Name);
        }
    }

    protected virtual void Remove(ITarget target) {
        bool isRemoved = AllTargets.Remove(target);
        if (isRemoved) {
            D.Log("No longer tracking target {0}. Distance = {1}.", target.Name, Vector3.Distance(target.Position, _transform.position));
            target.onItemDeath -= OnTargetDeath;
            target.onOwnerChanged -= OnTargetOwnerChanged;
        }
        else {
            D.Warn("Target {0} not present to be removed.", target.Name);
        }
    }

    protected virtual void OnTargetDeath(ITarget target) {
        Remove(target);
    }

    protected virtual void OnTargetOwnerChanged(ITarget target) { }

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

