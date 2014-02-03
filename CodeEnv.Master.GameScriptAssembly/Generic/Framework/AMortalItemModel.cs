﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemModel.cs
// Abstract base class for an AItem that can die.
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
/// Abstract base class for an AItem that can die. 
/// </summary>
public abstract class AMortalItemModel : AItemModel, IDisposable {

    public event Action<AMortalItemModel> onDeath;

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    protected IList<IDisposable> _subscribers;

    // Derived classes should call Subscribe() from Awake() after any required references are established

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    protected override void SubscribeToDataValueChanges() {
        _subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, OnHealthChanged));
    }

    protected virtual void OnHealthChanged() {
        //D.Log("{0} Health = {1}.", Data.Name, Data.Health);
        if (Data.Health <= Constants.ZeroF) {
            Die();
        }
    }

    protected virtual void Die() {
        D.Log("{0} is Dying!", Data.Name);
        var temp = onDeath;
        if (temp != null) {
            temp(this);
        }
        GameEventManager.Instance.Raise<MortalItemDeathEvent>(new MortalItemDeathEvent(this, this));
        // TODO derived classes should override to initiate Dying state in their state machine
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

