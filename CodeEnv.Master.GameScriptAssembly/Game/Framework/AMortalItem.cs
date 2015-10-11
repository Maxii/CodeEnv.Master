// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItem.cs
// Abstract class for AIntelItem's that can die.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class for AIntelItem's that can die.
/// </summary>
public abstract class AMortalItem : AIntelItem, IMortalItem {

    public event Action<IMortalItem> onDeathOneShot;

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    #region Initialization

    protected override void InitializeViewMembersWhenFirstDiscernibleToUser() {
        base.InitializeViewMembersWhenFirstDiscernibleToUser();
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, OnHealthChanged));
    }

    protected override EffectsManager InitializeEffectsManager() {
        return new MortalEffectsManager(this);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        Data.CommenceOperations();
    }

    protected override void OnDataSet() {
        base.OnDataSet();
        Data.PassiveCountermeasures.ForAll(cm => Attach(cm));
    }

    /// <summary>
    /// Attaches this passive countermeasure to this item.
    /// OPTIMIZE - currently does nothing.
    /// </summary>
    /// <param name="cm">The cm.</param>
    private void Attach(PassiveCountermeasure cm) {
        // IsActivated = true is set when item operations commences
    }

    /// <summary>
    /// Called when the item's health has changed. 
    /// NOTE: Donot use this to initiate the death of an item. That is handled in MortalItemModels as damage is taken which
    /// makes the logic behind dieing more visible and understandable. In the case of a UnitCommandModel, death occurs
    /// when the last Element has been removed from the Unit.
    /// </summary>
    protected virtual void OnHealthChanged() { }

    /// <summary>
    /// Initiates the death sequence of this MortalItem. This is the primary method
    /// to call to initiate death. Donot use OnDeath or set IsOperational or set a state of Dead.
    /// Note: the primary reason is to make sure IsOperational immediately reflects the death
    /// and can be used right away to check for it. Use of a state of Dead for the filter 
    /// can also work as it is changed immediately too. 
    /// <remarks>The previous implementation had IsOperational being set when the Dead 
    /// EnterState ran, which could be a whole frame later, given the way the state machine works. 
    /// This approach keeps IsOperational and Dead in sync.
    /// </remarks>
    /// </summary>
    protected void InitiateDeath() {
        D.Log("{0} is initiating death sequence.", FullName);
        IsOperational = false;
        SetDeadState();
        PrepareForOnDeathNotification();
        OnDeath();
        CleanupAfterOnDeathNotification();
    }

    /// <summary>
    ///Derived classes should set a state of Dead in their state machines.
    /// </summary>
    protected abstract void SetDeadState();

    /// <summary>
    /// Executes any preparation work prior to broadcasting the OnDeath event.
    /// </summary>
    protected virtual void PrepareForOnDeathNotification() {
        if (IsFocus) {
            References.MainCameraControl.CurrentFocus = null;
        }
        Data.PassiveCountermeasures.ForAll(cm => cm.IsActivated = false);
    }

    private void OnDeath() {
        if (onDeathOneShot != null) {
            onDeathOneShot(this);
            onDeathOneShot = null;
        }
    }

    /// <summary>
    /// Executes any cleanup work required after the OnDeath event has been broadcast.
    /// </summary>
    protected virtual void CleanupAfterOnDeathNotification() { }

    #endregion

    #region View Methods
    #endregion

    #region Mouse Events

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        __SimulateAttacked();
    }

    #endregion

    #region Attack Simulation

    public virtual void __SimulateAttacked() {
        float damageValue = UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints / 2F);
        TakeHit(new DamageStrength(damageValue, damageValue, damageValue));
    }

    #endregion

    #region Combat Support Methods

    public abstract void TakeHit(DamageStrength attackerWeaponStrength);

    /// <summary>
    /// Applies the damage to the Item and returns true if the Item survived the hit.
    /// </summary>
    /// <param name="damageSustained">The damage sustained.</param>
    /// <param name="damageSeverity">The damage severity.</param>
    /// <returns>
    ///   <c>true</c> if the Item survived.
    /// </returns>
    protected virtual bool ApplyDamage(DamageStrength damageSustained, out float damageSeverity) {
        var __damageTotal = damageSustained.Total;
        damageSeverity = Mathf.Clamp01(__damageTotal / Data.CurrentHitPoints);
        Data.CurrentHitPoints -= __damageTotal;
        if (Data.Health > Constants.ZeroPercent) {
            AssessCripplingDamageToEquipment(damageSeverity);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Assesses and applies any crippling damage to the item's equipment as a result of the hit.
    /// </summary>
    /// <param name="damageSeverity">The severity of the damage as a percentage of the item's hit points when hit.</param>
    protected virtual void AssessCripplingDamageToEquipment(float damageSeverity) {
        Arguments.ValidateForRange(damageSeverity, Constants.ZeroF, Constants.OneHundredPercent);
        //var passiveCMSurvivalChance = Constants.OneHundredPercent - damageSeverity;
        var passiveCmDamageChance = damageSeverity;
        //var operationalPassiveCMs = Data.PassiveCountermeasures.Where(cm => cm.IsOperational);
        var undamagedPassiveCMs = Data.PassiveCountermeasures.Where(cm => !cm.IsDamaged);
        undamagedPassiveCMs.ForAll(cm => cm.IsDamaged = RandomExtended.Chance(passiveCmDamageChance));
        //operationalPassiveCMs.ForAll(cm => cm.IsOperational = RandomExtended.Chance(passiveCMSurvivalChance));
    }

    #endregion

    protected void __DestroyMe(float delay = 0F, Action onCompletion = null) {
        UnityUtility.Destroy(gameObject, delay, onCompletion);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        //if (_showingJob != null) {
        //    _showingJob.Dispose();
        //}
        Data.Dispose();
    }

    #endregion

    #region Debug

    /// <summary>
    /// Logs the method name called. WARNING:  Coroutines showup as &lt;IEnumerator.MoveNext&gt; rather than the method name
    /// </summary>
    public override void LogEvent() {
        if (DebugSettings.Instance.EnableEventLogging) {
            var stackFrame = new System.Diagnostics.StackFrame(1);
            Debug.Log("{0}.{1}.{2}() called.".Inject(FullName, GetType().Name, stackFrame.GetMethod().Name));
        }
    }

    #endregion

}

