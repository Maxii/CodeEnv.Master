// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMortalItem.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
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
/// COMMENT 
/// </summary>
public abstract class AMortalItem : AItem, IMortalModel, IMortalTarget /*, IMortalViewable*/ {

    public event Action<MortalAnimations> onShowAnimation;  // not used
    public event Action<MortalAnimations> onStopAnimation;  // not used

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

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
        //_subscribers.Add(Data.SubscribeToPropertyChanged<AMortalItemData, IPlayer>(d => d.Owner, OnOwnerChanged));
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

    protected virtual void OnDeath() {
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

        if (IsFocus) { References.CameraControl.CurrentFocus = null; }
    }

    #endregion

    #region View Methods

    public abstract void OnShowCompletion();

    #region Animations

    // these must return onShowCompletion when finished
    private void ShowDying() {
        LogEvent();
        _showingJob = new Job(ShowingDying(), toStart: true);
    }

    // these run until finished with no requirement to return onShowCompletion
    private void ShowHit() {
        LogEvent();
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
        OnShowCompletion();
    }

    protected void ShowAnimation(MortalAnimations animation) {
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

    protected void StopAnimation(MortalAnimations animation) {
        if (_showingJob != null && _showingJob.IsRunning) {
            _showingJob.Kill();
            return;
        }
        //D.Warn("No Animation named {0} to stop.", animation.GetName());   // Commented out as most show Jobs not yet implemented
    }

    //protected void OnShowCompletion() {
    //    if (onShowCompletion != null) {
    //        onShowCompletion();
    //    }
    //}

    #endregion

    #endregion

    #region Mouse Events

    protected override void OnAltLeftClick() {
        base.OnAltLeftClick();
        __SimulateAttacked();
    }

    #endregion


    //protected void OnShowAnimation(MortalAnimations animation) {
    //    if (onShowAnimation != null) {
    //        onShowAnimation(animation);
    //    }
    //}

    //protected void OnStopAnimation(MortalAnimations animation) {
    //    if (onStopAnimation != null) {
    //        onStopAnimation(animation);
    //    }
    //}


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

    public virtual SpaceTopography Topography { get { return Data.Topography; } }

    #endregion

    #region IMortalTarget Members

    public event Action<IMortalTarget> onTargetDeathOneShot;

    /// <summary>
    /// Flag indicating whether the MortalItem is alive and operational.
    /// </summary>
    public bool IsAlive { get; protected set; }

    public string ParentName { get { return Data.ParentName; } }

    public abstract void TakeHit(CombatStrength weaponStrength);

    #endregion

    #region IMortalModel Members

    public event Action<IMortalModel> onDeathOneShot;

    #endregion

    //#region ICameraTargetable Members

    //public override bool IsEligible {
    //    get { return PlayerIntel.CurrentCoverage != IntelCoverage.None; }
    //}

    //#endregion

    //#region IMortalViewable Members

    //public event Action onShowCompletion;


    //public virtual void OnDeath() { }

    //#endregion

    #region Debug

    /// <summary>
    /// Logs the method name called. WARNING:  Coroutines showup as &lt;IEnumerator.MoveNext&gt; rather than the method name
    /// </summary>
    public override void LogEvent() {
        if (DebugSettings.Instance.EnableEventLogging) {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackFrame(1);
            Debug.Log("{0}.{1}.{2}() called.".Inject(FullName, GetType().Name, stackFrame.GetMethod().Name));
        }
    }

    #endregion


}

