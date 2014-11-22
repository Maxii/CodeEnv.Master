// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItem.cs
// Abstract base class for all items that can die.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for all items that can die.
/// </summary>
public abstract class AMortalItem : AItem, IMortalItem {

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    /// <summary>
    /// Flag indicating whether this MortalItem is alive and operating.
    /// </summary>
    public bool IsAlive { get; private set; }

    public AudioClip dying;
    public AudioClip hit;

    protected AudioSource _audioSource;
    protected Job _showingJob;

    #region Initialization

    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        _audioSource = UnityUtility.ValidateComponentPresence<AudioSource>(gameObject);
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, OnHealthChanged));
    }

    #endregion

    #region Model Methods

    public virtual void CommenceOperations() {
        IsAlive = true;
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
    /// to call to initiate death. Donot use OnDeath or set IsAlive or set a state of Dead.
    /// Note: the primary reason is to make sure IsAlive immediately reflects the death
    /// and can be used right away to check for it. Use of a state of Dead for the filter 
    /// can also work as it is changed immediately too. However, the previous implementation
    /// had IsAlive being set when the Dead EnterState ran, which can be a whole frame later,
    /// given the way the state machine works. This approach keeps isAlive and Dead in sync.
    /// </summary>
    protected virtual void InitiateDeath() {
        IsAlive = false;
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

    protected abstract void OnShowCompletion();

    #region Animations

    // these must call OnShowCompletion when finished
    private void ShowDying() {
        LogEvent();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingDying(), toStart: true, onJobComplete: (wasKilled) => {
            OnShowCompletion();
        });
    }


    // these run until finished with no requirement to call OnShowCompletion
    private void ShowHit() {
        LogEvent();
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
        }
        _showingJob = new Job(ShowingHit(), toStart: true);
    }


    protected virtual void ShowCmdHit() { LogEvent(); }

    protected virtual void ShowAttacking() { LogEvent(); }

    // these run continuously until they are stopped via StopAnimation() 
    protected virtual void ShowRepairing() { LogEvent(); }

    protected virtual void ShowRefitting() { LogEvent(); }

    protected virtual void ShowDisbanding() { LogEvent(); }

    private IEnumerator ShowingHit() {
        if (hit != null) {
            _audioSource.PlayOneShot(hit);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "hit");  
        yield return null;
        // does not use onShowCompletion
    }

    private IEnumerator ShowingDying() {
        if (dying != null) {
            _audioSource.PlayOneShot(dying);
        }
        //animation.Stop();
        //yield return UnityUtility.PlayAnimation(animation, "die");  // show debree particles for some period of time?
        yield return null;
    }

    public void ShowAnimation(MortalAnimations animation) {
        switch (animation) {
            case MortalAnimations.Dying:
                ShowDying();
                break;
            case MortalAnimations.Hit:
                ShowHit();
                break;
            case MortalAnimations.Attacking:
                ShowAttacking();
                break;
            case MortalAnimations.CmdHit:
                ShowCmdHit();
                break;
            case MortalAnimations.Disbanding:
                ShowDisbanding();
                break;
            case MortalAnimations.Refitting:
                ShowRefitting();
                break;
            case MortalAnimations.Repairing:
                ShowRepairing();
                break;
            case MortalAnimations.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(animation));
        }
    }

    public void StopAnimation(MortalAnimations animation) {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
            return;
        }
        //D.Warn("No Animation named {0} to stop.", animation.GetName());   // Commented out as most show Jobs not yet implemented
    }

    #endregion

    #endregion

    #region Mouse Events

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        __SimulateAttacked();
    }

    #endregion

    #region Attack Simulation

    public static ArmamentCategory[] __offensiveArmamentCategories = new ArmamentCategory[3] {  ArmamentCategory.MissileOffense,
                                                                                                ArmamentCategory.BeamOffense, 
                                                                                                ArmamentCategory.ParticleOffense 
    };

    public virtual void __SimulateAttacked() {
        TakeHit(new CombatStrength(RandomExtended<ArmamentCategory>.Choice(__offensiveArmamentCategories),
            UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints + 1F)));
    }

    public abstract void TakeHit(CombatStrength weaponStrength);

    #endregion

    #region Combat Support Methods

    /// <summary>
    /// Applies the damage to the Item. Returns true 
    /// if the Item survived the hit.
    /// </summary>
    /// <returns><c>true</c> if the Item survived.</returns>
    protected virtual bool ApplyDamage(float damage) {
        Data.CurrentHitPoints -= damage;
        return Data.Health > Constants.ZeroPercent;
    }

    #endregion

    protected void __DestroyMe(float delay, Action onCompletion = null) {
        UnityUtility.Destroy(gameObject, delay, onCompletion);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_showingJob != null) {
            _showingJob.Dispose();
        }
    }

    #endregion

    #region IMortalItem Members

    public event Action<IMortalItem> onDeathOneShot;

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

