// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AVectrosityBase.cs
// Base class for Vectrosity GameObjects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Base class for Vectrosity GameObjects.
/// Note: I've implemented this with a separate CoroutineScheduler
/// driven by the UnityUpdateEvent event so I can remember how to do it when I need
/// it outside of AMonoBehaviour classes. I don't strictly need it here.
/// </summary>
public abstract class AVectrosityBase : AMonoBehaviourBase, IDisposable {

    //protected static CoroutineScheduler _coroutineScheduler;
    //private static int _lastFrameCountAtUpdate;

    private string _lineName;
    public string LineName {
        get { return _lineName; }
        set { SetProperty<string>(ref _lineName, value, "LineName", OnNameChanged); }
    }

    protected VectorLine _line;

    //private GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        //if (_coroutineScheduler == null) {
        //    _coroutineScheduler = new CoroutineScheduler();
        //}
        //_eventMgr = GameEventManager.Instance;
        //Subscribe();
    }

    //private void Subscribe() {
    //    _eventMgr.AddListener<UnityUpdateEvent>(this, OnUpdate);
    //}

    private void OnNameChanged() {
        gameObject.name = LineName;
        if (_line != null) {
            _line.name = LineName;
        }
    }

    //private void OnUpdate(UnityUpdateEvent e) {
    //    int frameCount = Time.frameCount;
    //    if (frameCount != _lastFrameCountAtUpdate) {
    //        _coroutineScheduler.UpdateAllCoroutines(frameCount, Time.time);
    //        _lastFrameCountAtUpdate = frameCount;
    //    }
    //}

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    //private void Unsubscribe() {
    //    _eventMgr.RemoveListener<UnityUpdateEvent>(this, OnUpdate);
    //}

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
            //Unsubscribe();
            VectorLine.Destroy(ref _line);
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

