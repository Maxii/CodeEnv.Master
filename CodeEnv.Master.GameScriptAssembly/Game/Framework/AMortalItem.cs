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
public abstract class AMortalItem : AIntelItem, IMortalItem, IMortalItem_Ltd, IAttackable {

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
    /// Hook for derived classes to initiate transition to their DeadState.
    /// </summary>
    protected abstract void InitiateDeadState();

    /// <summary>
    /// Execute any preparation work that must occur prior to others hearing about this Item's death.
    /// The normal death shutdown process is handled by HandleDeath() which is called by the 
    /// Item's Dead_EnterState method up to a frame later.
    /// <remarks>Obsoleted 5.5.16 and replaced by HandleDeath().</remarks>
    /// </summary>
    [Obsolete]
    protected virtual void PrepareForDeathNotification() {
        // Moved this to HandleDeath 2.x.16 without noticing a problem when obsoleted 5.5.16
        //if (IsSelected) {
        //    SelectionManager.Instance.CurrentSelection = null; 
        //}
    }

    /// <summary>
    /// Handles the death shutdown process, called by the item's Dead state.
    /// </summary>
    protected virtual void HandleDeathFromDeadState() {
        Data.PassiveCountermeasures.ForAll(cm => cm.IsActivated = false);
        if (IsFocus) {
            HandleDeathWhileIsFocus();
        }
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
        if (IsHudShowing) {
            ShowHud(false);
        }
        (DisplayMgr as IMortalDisplayManager).HandleDeath();
    }

    /// <summary>
    /// Handles the death of this item when it is the focus. 
    /// </summary>
    private void HandleDeathWhileIsFocus() {
        D.Assert(IsFocus);
        References.MainCameraControl.CurrentFocus = null;
        AssignAlternativeFocusOnDeath();
    }

    /// <summary>
    /// Hook that allows derived classes to assign an alternative focus 
    /// when this mortal item dies while it is the focus.
    /// </summary>
    protected virtual void AssignAlternativeFocusOnDeath() { }

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
            D.Log(ShowDebugLog, "{0} is initiating death sequence.", FullName);
            InitiateDeadState();
            //PrepareForDeathNotification();
            OnDeath();
            //CleanupAfterDeathNotification();
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

    #region Attack Simulation

    public virtual void __SimulateAttacked() {
        float damageValue = UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints / 2F);
        TakeHit(new DamageStrength(damageValue, damageValue, damageValue));
    }

    #endregion

    #region State Machine Support Members

    #region Combat Support

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
        Utility.ValidateForRange(damageSeverity, Constants.ZeroPercent, Constants.OneHundredPercent);
        var passiveCmDamageChance = damageSeverity;
        var undamagedPassiveCMs = Data.PassiveCountermeasures.Where(cm => !cm.IsDamaged);
        undamagedPassiveCMs.ForAll(cm => cm.IsDamaged = RandomExtended.Chance(passiveCmDamageChance));
    }

    #endregion

    #endregion

    /// <summary>
    /// Execute any cleanup work that must occur immediately after others hearing about this Item's death.
    /// The normal death shutdown process is handled by HandleDeath() which is called by the 
    /// Item's Dead_EnterState method up to a frame later.
    /// <remarks>Obsoleted 5.5.16 and replaced by HandleDeath().</remarks>
    /// </summary>
    [Obsolete]
    protected virtual void CleanupAfterDeathNotification() { }

    /// <summary>
    /// Destroys this GameObject.
    /// </summary>
    /// <param name="delayInHours">The delay in hours.</param>
    /// <param name="onCompletion">Optional delegate that fires onCompletion.</param>
    protected virtual void DestroyMe(float delayInHours = Constants.ZeroF, Action onCompletion = null) {
        GameUtility.Destroy(gameObject, delayInHours, onCompletion);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        Data.Dispose();
    }

    #endregion

    #region IAttackable Members

    public bool IsAttackingAllowedBy(Player player) {
        if (!InfoAccessCntlr.HasAccessToInfo(player, AccessControlInfoID.Owner)) {
            return false;
        }
        return Owner.IsEnemyOf(player);
    }

    #endregion

}

