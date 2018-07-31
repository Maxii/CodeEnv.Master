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

    public event EventHandler __death;

    protected new AMortalItemData Data {
        get { return base.Data as AMortalItemData; }
        set { base.Data = value; }
    }

    ////[Obsolete]
    ////public IntVector3 SectorID { get { return Data.SectorID; } }

    /// <summary>
    /// Indicates this Item is dieing and about to be dead.
    /// <remarks>Set to <c>true</c> when the item should initiate dieing.</remarks>
    /// </summary>
    public bool IsDead {
        get { return Data.IsDead; }
        protected set { Data.IsDead = value; }
    }

    #region Initialization

    protected override void InitializeOnData() {
        base.InitializeOnData();
        Data.PassiveCountermeasures.ForAll(cm => Attach(cm));
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AMortalItemData, float>(d => d.Health, HealthPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanging<AMortalItemData, bool>(d => d.IsDead, IsDeadPropSettingHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AMortalItemData, bool>(d => d.IsDead, IsDeadPropSetHandler));
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
    /// Called just before IsDead becomes true.
    /// </summary>
    protected virtual void PrepareForDeath() {
        D.Assert(!IsDead);
    }

    /// <summary>
    /// The first prep method called after IsDead becomes true.
    /// </summary>
    protected virtual void PrepareForDeathSequence() { }

    /// <summary>
    /// Hook for derived classes to prepare for firing the death event.
    /// <remarks>The second prep method called after IsDead becomes true.</remarks>
    /// </summary>
    protected virtual void PrepareForOnDeath() { }

    /// <summary>
    /// Hook for derived classes to prepare for assigning the Dead State.
    /// <remarks>The third prep method called after IsDead becomes true.</remarks>
    /// </summary>
    protected virtual void PrepareForDeadState() { }

    /// <summary>
    /// Hook for derived classes to assign their Dead State.
    /// </summary>
    protected abstract void AssignDeadState();

    /// <summary>
    /// Handles the death shutdown process prior to beginning the
    /// death effect. Called by the item's Dead state.
    /// </summary>
    /// <summary>
    /// Hook for derived classes to prepare for any death visual and audio effects.
    /// <remarks>The fourth prep method called after IsDead becomes true.</remarks>
    /// </summary>
    protected virtual void PrepareForDeathEffect() { }

    /// <summary>
    /// Handles the death shutdown process after beginning the death effect. Called by the item's Dead state.
    /// <remarks>Death Effect will not begin if DisplayMgr has already disabled the display.
    /// When the display is disabled, the dead item thinks the primary mesh is no longer in the camera's LOS
    /// which results in IsVisualDetailDiscernibleToUser returning false, aka 'nobody can see it so don't show it'.
    ///</remarks>
    /// </summary>
    protected virtual void HandleDeathEffectBegun() {
        if (DisplayMgr != null) {
            (DisplayMgr as IMortalDisplayManager).HandleDeath();
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

    protected virtual void HandleDeathEffectFinished() {
        if (IsFocus) {
            GameReferences.MainCameraControl.CurrentFocus = null;
            AssignAlternativeFocusAfterDeathEffect();
        }
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
        if (IsHoveredHudShowing) {
            ShowHoveredHud(false);
        }
        // 7.24.17 HandleDeathForHighlights() moved to AMortalItem.HandleDeathEffectBegun 
        // as DisplayMgr.HandleDeath can destroy icons used for highlights
    }

    /// <summary>
    /// Hook that allows derived classes to assign an alternative focus 
    /// when this mortal item dies while it is the focus.
    /// </summary>
    protected virtual void AssignAlternativeFocusAfterDeathEffect() { }

    #region Event and Property Change Handlers

    private void IsDeadPropSettingHandler(bool incomingIsDead) {
        HandleIsDeadPropSetting(incomingIsDead);
    }

    private void IsDeadPropSetHandler() {
        HandleIsDeadPropSet();
    }

    private void HealthPropChangedHandler() {
        HandleHealthChanged();
    }

    protected void OnDeath() {
        if (deathOneShot != null) {
            deathOneShot(this, EventArgs.Empty);
            deathOneShot = null;
        }
        if (__death != null) {
            __death(this, EventArgs.Empty);
        }
    }

    #endregion

    private void HandleIsDeadPropSetting(bool incomingIsDead) {
        D.Assert(incomingIsDead);
        PrepareForDeath();
    }

    private void HandleIsDeadPropSet() {
        //D.Log(ShowDebugLog, "{0} is initiating death sequence.", DebugName);
        PrepareForDeathSequence();
        PrepareForOnDeath();
        OnDeath();
        PrepareForDeadState();
        AssignDeadState();
        // DeathEffect methods get called after this, from Dead_EnterState
    }

    /// <summary>
    /// Called when the item's health has changed. 
    /// NOTE: Do not use this to initiate the death of an item. That is handled in MortalItems as damage is taken which
    /// makes the logic behind dieing more visible and understandable. In the case of a UnitCommand, death occurs
    /// when the last Element has been removed from the Unit.
    /// </summary>
    protected virtual void HandleHealthChanged() { }

    protected sealed override void HandleAltLeftClick() {
        base.HandleAltLeftClick();
        if (!IsSelected) {
            D.Warn("{0} needs to be selected to Simulate Attack on itself.", DebugName);
            return;
        }
        __SimulateAttacked();
    }

    #region State Machine Support Members

    #region Combat Support

    public abstract void TakeHit(DamageStrength attackerWeaponStrength);

    #endregion

    #endregion

    /// <summary>
    /// Destroys this GameObject.
    /// </summary>
    /// <param name="delayInHours">The delay in hours.</param>
    /// <param name="onCompletion">Optional delegate that fires onCompletion.</param>
    protected virtual void DestroyMe(float delayInHours = Constants.ZeroF, Action onCompletion = null) {
        D.Log(ShowDebugLog, "{0} is being destroyed.", DebugName);
        if (gameObject == null) {
            D.Warn("{0} has already been destroyed.", DebugName);
            return;
        }
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

    #region Debug

    public virtual void __SimulateAttacked() {
        D.LogBold("{0} is having an attack simulated on itself.", DebugName);
        var damageCats = Enums<DamageCategory>.GetValues(excludeDefault: true);
        var damageCat = RandomExtended.Choice(damageCats);
        float damageValue = UnityEngine.Random.Range(Constants.ZeroF, Data.MaxHitPoints / 2F);
        TakeHit(new DamageStrength(damageCat, damageValue));
    }

    public void __LogDeathEventSubscribers() {
        if (__death != null) {
            IList<string> targetNames = new List<string>();
            var subscribers = __death.GetInvocationList();
            foreach (var sub in subscribers) {
                targetNames.Add(sub.Target.ToString());
            }
            Debug.LogFormat("{0}.__death event subscribers: {1}.", DebugName, targetNames.Concatenate());
        }
        else {
            Debug.LogFormat("{0}.__death event has no subscribers.", DebugName);
        }
    }


    #endregion

    #region IAttackable Members

    public bool IsAttackAllowedBy(Player attackingPlayer) {
        if (IsDead) {
            return false;
        }
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(attackingPlayer, ItemInfoID.Owner)) {
            return false;
        }
        return IsWarAttackAllowedBy(attackingPlayer) || IsColdWarAttackAllowedBy(attackingPlayer);
    }

    public bool IsColdWarAttackAllowedBy(Player attackingPlayer) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(attackingPlayer, ItemInfoID.Owner)) {
            return false;
        }

        if (Owner.IsRelationshipWith(attackingPlayer, DiplomaticRelationship.ColdWar)) {
            var sectorGrid = GameReferences.SectorGrid;
            IntVector3 itemsCurrentSectorID;
            if (sectorGrid.TryGetSectorIDContaining(Position, out itemsCurrentSectorID)) {
                // 4.12.17 OK to use ISector for owner access as this method really answers the question of
                // the attackingPlayer who would know if this item was in their territory // IMPROVE is there a better way?
                ISector itemsCurrentSector = sectorGrid.GetSector(itemsCurrentSectorID);
                if (itemsCurrentSector.Owner == attackingPlayer) {
                    // We are in ColdWar and this item is located in their territory so they can attack it
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsWarAttackAllowedBy(Player attackingPlayer) {
        if (!InfoAccessCntlr.HasIntelCoverageReqdToAccess(attackingPlayer, ItemInfoID.Owner)) {
            return false;
        }
        return Owner.IsAtWarWith(attackingPlayer);
    }

    #endregion

}

