// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItemModel.cs
// Abstract base class for an Item that can take damage and die.
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
/// Abstract base class for an Item that can take damage and die.
/// </summary>
public abstract class AMortalItemModel : AOwnedItemModel, IMortalModel, IMortalTarget {

    public event Action<MortalAnimations> onShowAnimation;
    public event Action<MortalAnimations> onStopAnimation;

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    protected override void Initialize() {
        IsAlive = true;
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

    protected virtual void OnDeath() {
        enabled = false;
        IsAlive = false;
        if (onTargetDeathOneShot != null) {
            onTargetDeathOneShot(this);
            onTargetDeathOneShot = null;
        }
        if (onDeathOneShot != null) {
            onDeathOneShot(this);
            onDeathOneShot = null;
        }
        // OPTIMIZE not clear this event will ever be used
        GameEventManager.Instance.Raise<MortalItemDeathEvent>(new MortalItemDeathEvent(this, this));
    }

    protected void OnShowAnimation(MortalAnimations animation) {
        if (onShowAnimation != null) {
            onShowAnimation(animation);
        }
    }

    protected void OnStopAnimation(MortalAnimations animation) {
        if (onStopAnimation != null) {
            onStopAnimation(animation);
        }
    }

    private void OnIsOperationalChanged() {
        //D.Log("{0}.IsOperational and collider.enabled changed to {1}.", FullName, IsOperational);
        collider.enabled = IsOperational;
    }

    public abstract void OnShowCompletion();

    #region Attack Simulation

    public static ArmamentCategory[] offensiveArmamentCategories = new ArmamentCategory[3] {    ArmamentCategory.BeamOffense, 
                                                                                                ArmamentCategory.MissileOffense, 
                                                                                                ArmamentCategory.ParticleOffense };
    public virtual void __SimulateAttacked() {
        TakeHit(new CombatStrength(RandomExtended<ArmamentCategory>.Choice(offensiveArmamentCategories),
            UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F)));
    }

    #endregion

    #region StateMachine Support Methods

    /// <summary>
    /// Applies the damage to the Item. Returns true 
    /// if the Item survived the hit.
    /// </summary>
    /// <returns><c>true</c> if the Item survived.</returns>
    protected virtual bool ApplyDamage(float damage) {
        Data.CurrentHitPoints -= damage;
        return Data.Health > Constants.ZeroF;
    }

    protected void DestroyMortalItem(float delayInSeconds) {
        new Job(DelayedDestroy(delayInSeconds), toStart: true, onJobComplete: (wasKilled) => {
            D.Log("{0} has been destroyed.", FullName);
        });
    }

    private IEnumerator DelayedDestroy(float delayInSeconds) {
        D.Log("{0}.DelayedDestroy({1}) called.", FullName, delayInSeconds);
        yield return new WaitForSeconds(delayInSeconds);
        Destroy(gameObject);
    }

    #endregion

    #region IDestinationTarget Members

    public Vector3 Position { get { return Data.Position; } }

    //public virtual bool IsMobile { get { return false; } }

    public SpaceTopography Topography { get { return Data.Topography; } }

    #endregion

    #region IMortalTarget Members

    public event Action<IMortalTarget> onTargetDeathOneShot;

    public bool IsAlive { get; protected set; }

    public abstract void TakeHit(CombatStrength weaponStrength);

    public string ParentName { get { return Data.ParentName; } }

    #endregion

    #region IMortalModel Members

    public event Action<IMortalModel> onDeathOneShot;

    private bool _isOperational;
    public bool IsOperational {
        get { return _isOperational; }
        set { SetProperty<bool>(ref _isOperational, value, "IsOperational", OnIsOperationalChanged); }
    }

    #endregion

}

