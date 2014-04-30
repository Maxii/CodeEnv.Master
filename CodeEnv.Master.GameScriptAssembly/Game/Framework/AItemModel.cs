﻿// --------------------------------------------------------------------------------------------------------------------
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
/// The abstract data-holding base class for all solid and non-solid objects in the game.
/// </summary>
public abstract class AItemModel : AMonoBase, IModel, IDestinationTarget, IDisposable {

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

    protected override void Start() {
        base.Start();
        Initialize();
        //EnableView();
    }

    /// <summary>
    /// Called from Start(), just before EnableView(). Do any 
    /// initialization required before the corresponding view is enabled.
    /// </summary>
    protected abstract void Initialize();

    /// <summary>
    /// Enables the corresponding View for this model. Called after Initialize().
    /// </summary>
    private void EnableView() {
        gameObject.GetSafeInterface<IViewable>().enabled = true;
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
    /// Subscribes to changes to values contained in Data. 
    /// </summary>
    protected virtual void SubscribeToDataValueChanges() {
        D.Assert(_subscribers != null);
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.Name, OnNamingChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.OptionalParentName, OnNamingChanged));
    }

    /// <summary>
    /// Called when either the Item name or parentName is changed.
    /// </summary>
    protected virtual void OnNamingChanged() { }

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

    #region IModel Members

    public Transform Transform { get { return _transform; } }

    #endregion

    #region IDestinationTarget Members

    public virtual string FullName {
        get {
            if (Data != null) {
                return Data.FullName;
            }
            return _transform.name + " (from transform)";
        }
    }

    public Vector3 Position { get { return Data.Position; } }

    private float _radius;
    public float Radius {
        get {
            D.Assert(_radius != Constants.ZeroF);
            return _radius;
        }
        protected set { _radius = value; }
    }

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

