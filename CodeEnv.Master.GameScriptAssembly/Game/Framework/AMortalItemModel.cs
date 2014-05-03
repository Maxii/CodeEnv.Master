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
public abstract class AMortalItemModel : AItemModel, IMortalModel, IMortalTarget {

    public event Action<MortalAnimations> onShowAnimation;
    public event Action<MortalAnimations> onStopAnimation;

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        // NOTE: MortalItemModel Planetoids, Ships and Facilities have their collider sizes preset in their prefabs so Radius can be set from the collider
        // The radius of a Command has nothing to do with the size of its collider. Instead, they reset this value to the Radius of their HQElement
        // The radius of a System, Star and Universe Center are constants and held in TempGameValues. They each set their own radius on Awake().
        Radius = collider.bounds.extents.magnitude;
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, OnHealthChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, IPlayer>(d => d.Owner, OnOwnerChanged));
    }

    /// <summary>
    /// Called when the item's health has changed. 
    /// NOTE: Donot use this to initiate the death of an item. That is handled in MortalItemModels as damage is taken which
    /// makes the logic behind dieing more visible and understandable. In the cae of a UnitCommandModel, death occurs
    /// when the last Element has been removed from the Unit.
    /// </summary>
    protected virtual void OnHealthChanged() { }

    protected virtual void OnOwnerChanged() {
        var temp = onOwnerChanged;
        if (temp != null) {
            temp(this);
        }
    }

    protected virtual void OnItemDeath() {
        enabled = false;
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
        D.Log("{0}.DelayedDestroy({1}).", FullName, delayInSeconds);
        yield return new WaitForSeconds(delayInSeconds);
        Destroy(gameObject);
    }

    #endregion

    #region IMortalTarget Members

    public event Action<IMortalModel> onItemDeath;

    public event Action<IMortalModel> onOwnerChanged;

    public bool IsDead { get; private set; }

    public override bool IsMovable { get { return true; } }

    public abstract void TakeDamage(float damage);

    public IPlayer Owner { get { return Data.Owner; } }

    public float MaxWeaponsRange { get { return Data.MaxWeaponsRange; } }

    public string ParentName { get { return Data.ParentName; } }

    #endregion

}

