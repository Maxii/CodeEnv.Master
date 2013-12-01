// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Item.cs
// The instantiable, data-holding base class for all objects in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The instantiable, data-holding base class for all objects in the game.
/// </summary>
public abstract class AItem : AMonoBase, IDisposable {

    private Data _data;
    /// <summary>
    /// Gets or sets the data for this item. Clients are responsible for setting in the right sequence as 
    /// one data can be dependant on another data.
    /// </summary>
    public Data Data {
        get { return _data; }
        set { SetProperty<Data>(ref _data, value, "Data", OnDataChanged, OnDataChanging); }
    }

    /// <summary>
    /// The radius in units of the conceptual 'globe' that encompasses this gameObject.
    /// Used by ship and fleet navigators so they don't run into an item as they approach.
    /// </summary>
    public float Radius { get; set; }

    protected IList<IDisposable> _subscribers;

    protected override void Awake() {
        base.Awake();
        // Subscribe(); derived classes should initiate Subscribe when their required references are established
        enabled = false;
    }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        // Delaying subscriptions from Data until Data is initialized
    }

    private void OnDataChanging(Data newData) {
        newData.Transform = _transform; // assign our transform to Data
    }

    protected virtual void OnDataChanged() {
        SubscribeToDataValueChanges();
    }

    protected virtual void OnHealthChanged() {
        //D.Log("{0} Health = {1}.", Data.Name, Data.Health);
        if (Data.Health <= Constants.ZeroF) {
            Die();
        }
    }

    protected virtual void SubscribeToDataValueChanges() {
        _subscribers.Add(Data.SubscribeToPropertyChanged<Data, float>(d => d.Health, OnHealthChanged));
    }

    protected virtual void Die() {
        D.Log("{0} has Died!", Data.Name);
        GameEventManager.Instance.Raise<ItemDeathEvent>(new ItemDeathEvent(this));
        Destroy(gameObject);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    protected virtual void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
    }

    protected virtual void Unsubscribe() {
        _subscribers.ForAll(d => d.Dispose());
        _subscribers.Clear();
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

