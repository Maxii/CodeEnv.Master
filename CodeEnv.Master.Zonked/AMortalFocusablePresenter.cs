// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalFocusablePresenter.cs
// An abstract base MVPresenter associated with AMortalItem.
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

/// <summary>
/// An abstract base MVPresenter associated with AMortalItem.
/// </summary>
public abstract class AMortalFocusablePresenter : AFocusableItemPresenter, IDisposable {

    protected GameEventManager _eventMgr;
    protected IList<IDisposable> _subscribers;

    public AMortalItemPresenter(IViewable view)
        : base(view) {
        _eventMgr = GameEventManager.Instance;
        Subscribe();
    }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        _eventMgr.AddListener<ItemDeathEvent>(this, OnItemDeath);
    }

    protected abstract void OnItemDeath(ItemDeathEvent e);

    protected virtual void CleanupOnDeath() {
        if ((View as ICameraFocusable).IsFocus) {
            CameraControl.Instance.CurrentFocus = null;
        }
    }

    protected virtual void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    protected virtual void Unsubscribe() {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
        _eventMgr.RemoveListener<ItemDeathEvent>(this, OnItemDeath);
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

