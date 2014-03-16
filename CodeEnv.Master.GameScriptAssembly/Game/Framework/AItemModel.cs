// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItemModel.cs
// The abstract data-holding base class for all solid and non-solid objects in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The abstract data-holding base class for all solid and non-solid objects in the game.
/// </summary>
public abstract class AItemModel : AMonoBase, IModel, IDisposable {

    private AItemData _data;
    /// <summary>
    /// Gets or sets the data for this item. Clients are responsible for setting in the right sequence as 
    /// one data can be dependant on another data.
    /// </summary>
    public AItemData Data {
        get { return _data; }
        set {
            if (_data == value) { return; }
            _data = value;
            OnDataChanged();
        }
    }

    protected IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        enabled = false;
        // Derived classes should call Subscribe() from Awake() after any required references are established
    }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    protected virtual void OnDataChanged() {
        Data.Transform = _transform;
        SubscribeToDataValueChanges();
    }

    /// <summary>
    /// Placeholder for subscribing to changes to values contained in Data. 
    /// Does nothing.
    /// </summary>
    protected virtual void SubscribeToDataValueChanges() { }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    protected virtual void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    protected virtual void Unsubscribe() {
        if (_subscribers != null) { // allows derived classes to not subscribe if they don't need it
            _subscribers.ForAll(d => d.Dispose());
            _subscribers.Clear();
        }
    }

    #region IDestinationItem Members

    public string Name {
        get {
            if (Data != null) {
                return Data.Name;
            }
            return _transform.name + " (from transform)";
        }
    }

    public Vector3 Position { get { return Data.Position; } }

    /// <summary>
    /// The radius in units of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public float Radius { get; set; }

    public virtual bool IsMovable { get { return false; } }

    #endregion

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;
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
        if (alreadyDisposed) {
            return;
        }

        _isDisposing = isDisposing;
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

