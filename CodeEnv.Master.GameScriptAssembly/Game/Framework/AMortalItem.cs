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

    /// <summary>
    /// Debug flag indicating whether to show the D.Log for this item.
    /// <remarks>Requires #define DEBUG_LOG to be compiled.</remarks>
    /// </summary>
    public bool showDebugLog = false;

    public event EventHandler deathOneShot;

    public new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    public Index3D SectorIndex { get { return Data.SectorIndex; } }

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        Data.PassiveCountermeasures.ForAll(cm => Attach(cm));
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, HealthPropChangedHandler));
    }

    protected override EffectsManager InitializeEffectsManager() {
        return new MortalEffectsManager(this);
    }

    #endregion

    /// <summary>
    /// Attaches this passive countermeasure to this item.
    /// OPTIMIZE - currently does nothing.
    /// </summary>
    /// <param name="cm">The cm.</param>
    private void Attach(PassiveCountermeasure cm) {
        // cm.IsActivated = true is set when item operations commences
    }

    /*******************************************************************************************************************************
     * HOW TO INITIATE DEATH: Set IsOperational to false. Do not use OnDeath, HandleDeath or set a state of Dead. 
     * The primary reason is to make sure IsOperational immediately reflects the death and can be used right away to check for it. 
     * Use of CurrentState == Dead can also be checked as it is changed immediately too. 
     * The previous implementation had IsOperational being set when the Dead EnterState ran, which could be a whole 
     * frame later, given the way the state machine works. This approach keeps IsOperational and Dead in sync.
     ********************************************************************************************************************************/

    /// <summary>
    ///Derived classes should set a state of Dead in their state machines.
    /// </summary>
    protected abstract void SetDeadState();

    /// <summary>
    /// Execute any preparation work that must occur prior to others hearing about this Item's death.
    /// The normal death shutdown process is handled by HandleDeath() which is called by the 
    /// Item's Dead_EnterState method up to a frame later.
    /// </summary>
    protected virtual void PrepareForDeathNotification() {
        //if (IsSelected) {
        //    SelectionManager.Instance.CurrentSelection = null;  // UNCLEAR can this wait until Dead_EnterState calls HandleDeath?
        //}
    }

    /// <summary>
    /// Handles the death shutdown process, called by Dead_EnterState.
    /// </summary>
    protected virtual void HandleDeath() {
        Data.PassiveCountermeasures.ForAll(cm => cm.IsActivated = false);
        if (IsFocus) {
            HandleDeathWhileIsFocus();
        }
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;  // UNCLEAR can this wait until Dead_EnterState calls HandleDeath?
        }
    }

    /// <summary>
    /// Handles the death of this item when it is the focus. Allows override
    /// by AUnitElementItem so it can assign its command as the focus to replace it.
    /// </summary>
    protected virtual void HandleDeathWhileIsFocus() {
        D.Assert(IsFocus);
        References.MainCameraControl.CurrentFocus = null;
    }

    #region Event and Property Change Handlers

    /// <summary>
    /// Called when the item's health has changed. 
    /// NOTE: Donot use this to initiate the death of an item. That is handled in MortalItems as damage is taken which
    /// makes the logic behind dieing more visible and understandable. In the case of a UnitCommand, death occurs
    /// when the last Element has been removed from the Unit.
    /// </summary>
    protected virtual void HealthPropChangedHandler() { }

    protected sealed override void IsOperationalPropChangedHandler() {
        base.IsOperationalPropChangedHandler();
        if (!IsOperational) {
            D.Log("{0} is initiating death sequence.", FullName);
            SetDeadState();
            PrepareForDeathNotification();
            OnDeath();
            CleanupAfterDeathNotification();
            // HandleDeath gets called after this, from Dead_EnterState
        }
    }

    protected override void HandleAltLeftClick() {
        base.HandleAltLeftClick();
        __SimulateAttacked();
    }

    private void OnDeath() {
        if (deathOneShot != null) {
            deathOneShot(this, new EventArgs());
            deathOneShot = null;
        }
    }

    #endregion

    /// <summary>
    /// Execute any cleanup work that must occur immediately after others hearing about this Item's death.
    /// The normal death shutdown process is handled by HandleDeath() which is called by the 
    /// Item's Dead_EnterState method up to a frame later.
    /// </summary>
    protected virtual void CleanupAfterDeathNotification() { }

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
        Arguments.ValidateForRange(damageSeverity, Constants.ZeroPercent, Constants.OneHundredPercent);
        var passiveCmDamageChance = damageSeverity;
        var undamagedPassiveCMs = Data.PassiveCountermeasures.Where(cm => !cm.IsDamaged);
        undamagedPassiveCMs.ForAll(cm => cm.IsDamaged = RandomExtended.Chance(passiveCmDamageChance));
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
        if (DebugSettings.Instance.EnableEventLogging && showDebugLog) {
            var stackFrame = new System.Diagnostics.StackFrame(1);
            string name = Utility.CheckForContent(FullName) ? FullName : transform.name + "(from transform)";
            Debug.Log("{0}.{1}.{2}() beginning execution.".Inject(name, GetType().Name, stackFrame.GetMethod().Name));
        }
    }

    #endregion

}

