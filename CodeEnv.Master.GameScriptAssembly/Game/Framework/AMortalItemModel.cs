// --------------------------------------------------------------------------------------------------------------------
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
using System.Collections;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for an AItem that can die. 
/// </summary>
public abstract class AMortalItemModel : AItemModel, ITarget, IDisposable {

    public event Action<MortalAnimations> onShowAnimation;
    public event Action<MortalAnimations> onStopAnimation;

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    protected IList<IDisposable> _subscribers;

    protected override void Start() {
        base.Start();
        Initialize();
    }

    protected abstract void Initialize();

    // Derived classes should call Subscribe() from Awake() after any required references are established

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, OnHealthChanged));
    }

    /// <summary>
    /// Called when the item's health has changed. 
    /// NOTE: Donot use this to initiate the death of an item. That is handled in MortalItemModels as damage is taken which
    /// makes the logic behind dieing more visible and understandable. In the cae of a UnitCommandModel, death occurs
    /// when the last Element has been removed from the Unit.
    /// </summary>
    protected virtual void OnHealthChanged() { }

    protected void OnItemDeath() {
        IsDead = true;
        var temp = onItemDeath;
        if (temp != null) {
            temp(this);
        }
        // OPTIMIZE not clear this event will ever be used
        GameEventManager.Instance.Raise<MortalItemDeathEvent>(new MortalItemDeathEvent(this, this));
    }

    protected void OnShowAnimation(MortalAnimations animation) {
        var temp = onShowAnimation;
        if (temp != null) {
            temp(animation);
        }
    }

    protected void OnStopAnimation(MortalAnimations animation) {
        var temp = onStopAnimation;
        if (temp != null) {
            temp(animation);
        }
    }

    public abstract void OnShowCompletion();

    public virtual void __SimulateAttacked() {
        TakeDamage(UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F));
    }

    #region StateMachine Support Methods

    /// <summary>
    /// Applies the damage to the Item. Returns true 
    /// if the Item survived the hit.
    /// </summary>
    /// <returns><c>true</c> if the Item survived.</returns>
    protected bool ApplyDamage(float damage) {
        Data.CurrentHitPoints -= damage;
        return Data.Health > Constants.ZeroF;
    }

    protected IEnumerator DelayedDestroy(float delayInSeconds) {
        D.Log("{0}.DelayedDestroy({1}).", Data.Name, delayInSeconds);
        yield return new WaitForSeconds(delayInSeconds);
        D.Log("{0} GameObject being destroyed.", Data.Name);
        Destroy(gameObject);
    }

    #endregion

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

    #region ITarget Members

    public event Action<ITarget> onItemDeath;

    public bool IsDead { get; private set; }

    public override bool IsMovable { get { return true; } }

    public abstract void TakeDamage(float damage);

    #endregion

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

