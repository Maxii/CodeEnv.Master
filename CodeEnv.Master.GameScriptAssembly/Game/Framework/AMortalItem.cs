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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
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

    public IntVector3 SectorID { get { return Data.SectorID; } }

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
    /// Execute any preparation work that must occur prior to others hearing about this Item's death.
    /// The normal death shutdown process is handled by HandleDeathXXX() which is called by the 
    /// Item's Dead_EnterState method up to a frame later.
    /// </summary>
    protected virtual void PrepareForDeathNotification() {
        Data.PassiveCountermeasures.ForAll(cm => cm.IsActivated = false);
    }

    /// <summary>
    /// Hook for derived classes to initiate transition to their DeadState.
    /// </summary>
    protected abstract void InitiateDeadState();

    /// <summary>
    /// Handles the death shutdown process prior to beginning the
    /// death effect. Called by the item's Dead state.
    /// </summary>
    protected virtual void HandleDeathBeforeBeginningDeathEffect() {
        if (IsFocus) {
            References.MainCameraControl.CurrentFocus = null;
            AssignAlternativeFocusOnDeath();
        }
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
        if (IsHudShowing) {
            ShowHud(false);
        }
        HandleDeathForHighlights();
    }

    private void HandleDeathForHighlights() {
        var highlightMgrIDs = Enums<HighlightMgrID>.GetValues(excludeDefault: true);
        foreach (var mgrID in highlightMgrIDs) {
            if (DoesHighlightMgrExist(mgrID)) {
                var highlightMgr = GetHighlightMgr(mgrID);
                highlightMgr.HandleClientDeath();
            }
        }
    }

    /// <summary>
    /// Hook that allows derived classes to assign an alternative focus 
    /// when this mortal item dies while it is the focus.
    /// </summary>
    protected virtual void AssignAlternativeFocusOnDeath() { }

    /// <summary>
    /// Handles the death shutdown process after beginning the death effect. Called by the item's Dead state.
    /// <remarks>Death Effect will not begin if DisplayMgr has already disabled the display.
    /// When the display is disabled, the dead item thinks the primary mesh is no longer in the camera's LOS
    /// which results in IsVisualDetailDiscernibleToUser returning false, aka 'nobody can see it so don't show it'.
    ///</remarks>
    /// </summary>
    protected virtual void HandleDeathAfterBeginningDeathEffect() {
        if (DisplayMgr != null) {
            (DisplayMgr as IMortalDisplayManager).HandleDeath();
        }
    }

    #region Event and Property Change Handlers

    private void HealthPropChangedHandler() {
        HandleHealthChanged();
    }

    /// <summary>
    /// Called when the item's health has changed. 
    /// NOTE: Do not use this to initiate the death of an item. That is handled in MortalItems as damage is taken which
    /// makes the logic behind dieing more visible and understandable. In the case of a UnitCommand, death occurs
    /// when the last Element has been removed from the Unit.
    /// </summary>
    protected virtual void HandleHealthChanged() { }

    protected sealed override void HandleIsOperationalChanged() {
        base.HandleIsOperationalChanged();
        if (!IsOperational) {
            //D.Log(ShowDebugLog, "{0} is initiating death sequence.", DebugName);
            PrepareForDeathNotification();
            OnDeath();
            InitiateDeadState();
            // HandleDeathXXX() gets called after this, from Dead_EnterState
        }
    }

    protected override void HandleAltLeftClick() {
        base.HandleAltLeftClick();
        if (!IsSelected) {
            D.Warn("{0} needs to be selected to Simulate Attack on itself.", DebugName);
            return;
        }
        __SimulateAttacked();
    }

    private void OnDeath() {
        if (deathOneShot != null) {
            deathOneShot(this, EventArgs.Empty);
            deathOneShot = null;
        }
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
        var undamagedDamageablePassiveCMs = Data.PassiveCountermeasures.Where(cm => cm.IsDamageable && !cm.IsDamaged);
        undamagedDamageablePassiveCMs.ForAll(cm => cm.IsDamaged = RandomExtended.Chance(passiveCmDamageChance));
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
        D.Log("{0} is being destroyed.", DebugName);
        GameUtility.Destroy(gameObject, delayInHours, onCompletion);
    }

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (Data != null) {
            Data.Dispose();
        }
    }

    #endregion

    #region Nested Classes

    protected enum FsmTgtEventSubscriptionMode {
        None,
        TargetDeath,
        InfoAccessChg,
        OwnerChg
    }

    /// <summary>
    /// IEqualityComparer for FsmTgtEventSubscriptionMode. 
    /// <remarks>For use when FsmTgtEventSubscriptionMode is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    protected class FsmTgtEventSubscriptionModeEqualityComparer : IEqualityComparer<FsmTgtEventSubscriptionMode> {

        public static readonly FsmTgtEventSubscriptionModeEqualityComparer Default = new FsmTgtEventSubscriptionModeEqualityComparer();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<FsmTgtEventSubscriptionMode> Members

        public bool Equals(FsmTgtEventSubscriptionMode value1, FsmTgtEventSubscriptionMode value2) {
            return value1 == value2;
        }

        public int GetHashCode(FsmTgtEventSubscriptionMode value) {
            return value.GetHashCode();
        }

        #endregion

    }

    #endregion

    #region Debug

    public virtual void __SimulateAttacked() {
        D.LogBold("{0} is having an attack simulated on itself.", DebugName);
        float damageValue = UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints / 2F);
        TakeHit(new DamageStrength(damageValue, damageValue, damageValue));
    }

    #endregion

    #region IAttackable Members

    public bool IsAttackByAllowed(Player attackingPlayer) {
        if (!InfoAccessCntlr.HasAccessToInfo(attackingPlayer, ItemInfoID.Owner)) {
            return false;
        }
        return IsWarAttackByAllowed(attackingPlayer) || IsColdWarAttackByAllowed(attackingPlayer);
    }

    public bool IsColdWarAttackByAllowed(Player attackingPlayer) {
        if (!InfoAccessCntlr.HasAccessToInfo(attackingPlayer, ItemInfoID.Owner)) {
            return false;
        }
        return Owner.IsRelationshipWith(attackingPlayer, DiplomaticRelationship.ColdWar);   // TODO add test for owner's territory
    }

    public bool IsWarAttackByAllowed(Player attackingPlayer) {
        if (!InfoAccessCntlr.HasAccessToInfo(attackingPlayer, ItemInfoID.Owner)) {
            return false;
        }
        return Owner.IsAtWarWith(attackingPlayer);
    }

    #endregion

}

