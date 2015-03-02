﻿// --------------------------------------------------------------------------------------------------------------------
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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
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

    private IAnimator _animator;

    #region Initialization

    protected override void InitializeViewMembersOnDiscernible() {
        base.InitializeViewMembersOnDiscernible();
        _animator = InitializeAnimator();
        _animator.onAnimationFinished += OnShowCompletion;
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, OnHealthChanged));
    }

    protected virtual IAnimator InitializeAnimator() {
        var animation = gameObject.GetComponentInImmediateChildren<Animation>();
        var audioSource = gameObject.GetComponent<AudioSource>();
        return new MortalItemAnimator(animation, audioSource);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        Data.Countermeasures.ForAll(cm => cm.IsOperational = true);
    }

    public void AddCountermeasure(CountermeasureStat cmStat) {
        Countermeasure countermeasure = new Countermeasure(cmStat);
        Data.AddCountermeasure(countermeasure);
        if (IsOperational) {
            // we have already commenced operations so start the new countermeasure
            // countermeasures added before operations have commenced are started when operations commence
            countermeasure.IsOperational = true;
        }
    }

    public void RemoveCountermeasure(Countermeasure countermeasure) {
        D.Assert(IsOperational);
        countermeasure.IsOperational = false;
        Data.RemoveCountermeasure(countermeasure);
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
    /// can also work as it is changed immediately too. However, the previous implementation
    /// had IsOperational being set when the Dead EnterState ran, which can be a whole frame later,
    /// given the way the state machine works. This approach keeps IsOperational and Dead in sync.
    /// </summary>
    protected virtual void InitiateDeath() {
        D.Log("{0}.InitiateDeath() called.", FullName);
        IsOperational = false;
        OnDeath();
    }

    protected virtual void OnDeath() {
        if (onDeathOneShot != null) {
            onDeathOneShot(this);
            onDeathOneShot = null;
        }
        if (IsFocus) { References.MainCameraControl.CurrentFocus = null; }
    }

    #endregion

    #region View Methods

    public abstract void OnShowCompletion();

    protected void StartAnimation(MortalAnimations animationID) {
        if (_animator != null) {
            _animator.Start(animationID);
        }
    }

    protected void StopAnimation(MortalAnimations animationID) {
        if (_animator != null) {
            _animator.Stop(animationID);
        }
    }

    #endregion

    #region Mouse Events

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        __SimulateAttacked();
    }

    #endregion

    #region Attack Simulation

    public virtual void __SimulateAttacked() {
        TakeHit(new CombatStrength(Enums<ArmamentCategory>.GetRandom(excludeDefault: true),
            UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F)));
    }

    #endregion

    #region Combat Support Methods

    public abstract void TakeHit(CombatStrength attackerWeaponStrength);

    /// <summary>
    /// Applies the damage to the Item and returns true if the Item survived the hit.
    /// </summary>
    /// <param name="damageSustained">The damage sustained.</param>
    /// <param name="damageSeverity">The damage severity.</param>
    /// <returns>
    ///   <c>true</c> if the Item survived.
    /// </returns>
    protected virtual bool ApplyDamage(CombatStrength damageSustained, out float damageSeverity) {
        var __combinedDamage = damageSustained.Combined;
        damageSeverity = Mathf.Clamp01(__combinedDamage / Data.CurrentHitPoints);
        Data.CurrentHitPoints -= __combinedDamage;
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
        Arguments.ValidateForRange(damageSeverity, Constants.ZeroF, Constants.OneF);
        var operationalCountermeasures = Data.Countermeasures.Where(cm => cm.IsOperational);
        operationalCountermeasures.ForAll(cm => cm.IsOperational = RandomExtended<bool>.Chance(damageSeverity));
    }

    #endregion

    protected void __DestroyMe(float delay, Action onCompletion = null) {
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

