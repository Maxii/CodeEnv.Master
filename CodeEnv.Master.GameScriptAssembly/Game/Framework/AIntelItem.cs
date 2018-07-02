// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItem.cs
// Abstract class for ADiscernibleItem's that have knowledge of each player's IntelCoverage.
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

/// <summary>
/// Abstract class for ADiscernibleItem's that have knowledge of each player's IntelCoverage.
/// </summary>
public abstract class AIntelItem : ADiscernibleItem, IIntelItem, IIntelItem_Ltd, IDetectionHandlerClient {

    public IntelCoverage UserIntelCoverage { get { return Data.GetIntelCoverage(_gameMgr.UserPlayer); } }

    protected new AIntelItemData Data {
        get { return base.Data as AIntelItemData; }
        set { base.Data = value; }
    }

    #region Initialization

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        Data.intelCoverageChanged += IntelCoverageChangedEventHandler;
    }

    #endregion

    public IntelCoverage GetIntelCoverage(Player player) { return Data.GetIntelCoverage(player); }

    /// <summary>
    /// Sets the Intel coverage for this player. 
    /// <remarks>Convenience method for clients who don't care whether the value was accepted or not.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    public void SetIntelCoverage(Player player, IntelCoverage newCoverage) {
        Data.SetIntelCoverage(player, newCoverage);
    }


    /// <summary>
    /// Attempts to change the IntelCoverage of this Item to newCoverage. Returns <c>true</c> if the coverage
    /// was changed, false if not. In both cases, resultingCoverage will reflect the currentCoverage, changed or not.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    /// <param name="resultingCoverage">The resulting coverage.</param>
    /// <returns></returns>
    public bool TryChangeIntelCoverage(Player player, IntelCoverage newCoverage, out IntelCoverage resultingCoverage) {
        return Data.TryChangeIntelCoverage(player, newCoverage, out resultingCoverage);
    }

    protected sealed override void AssessIsDiscernibleToUser() {
        // 11.13.16 isInMainCameraLOS must start true to initialize DisplayMgr when UserIntelCoverage > None
        // as this assessment is only called from CommenceOperations, DisplayMgr or when UserIntelCoverage changes. 
        // Without starting true, if UserIntelCoverage starts > None (DebugControls.FullIntelOfDetectedItems or User-owned item), 
        // the assessment wouldn't be done again until coverage changed again, if ever.
        var isInMainCameraLOS = DisplayMgr != null ? DisplayMgr.IsInMainCameraLOS : true;
        IsDiscernibleToUser = isInMainCameraLOS && UserIntelCoverage > IntelCoverage.None;
    }

    #region Event and Property Change Handlers

    private void IntelCoverageChangedEventHandler(object sender, AIntelItemData.IntelCoverageChangedEventArgs e) {
        HandleIntelCoverageChanged(e.Player);
    }

    #endregion

    private void HandleIntelCoverageChanged(Player playerWhosCoverageChgd) {
        //D.Log(ShowDebugLog, "{0}.IntelCoverageChangedHandler() called. {1}'s new IntelCoverage = {2}.", DebugName, playerWhosCoverageChgd.Name, GetIntelCoverage(playerWhosCoverageChgd));

        // 6.28.18 AssessAwarenessOfItemFor(playerWhosCoverageChgd); moved to Data

        if (IsOperational) {    // Will be called during FinalInitialize if Item should be IntelCoverage.Comprehensive
            if (playerWhosCoverageChgd == _gameMgr.UserPlayer) {
                HandleUserIntelCoverageChanged();
            }

            Player playerWhosInfoAccessChgd = playerWhosCoverageChgd;
            HandleInfoAccessChangedFor(playerWhosInfoAccessChgd);
            OnInfoAccessChanged(playerWhosInfoAccessChgd);
        }
    }

    /// <summary>
    /// Hook for derived classes that have work to do when InfoAccess to this
    /// Item for <c>player</c> changes. Default does nothing.
    /// <remarks>Used by detectable Items to cause an undetectable Item to assess
    /// whether they should fire their own InfoAccessChanged event.</remarks>
    /// <remarks>3.22.17 Currently, SystemItem, SettlementCmdItem and Sector are
    /// undetectable Items that CAN choose to make their Owner accessible at 
    /// IntelCoverage.Basic. In SettlementCmd's and Sector's case, they will 
    /// make their Owner accessible if their System does. In System's case
    /// it makes its Owner accessible if its Star or any of its Planetoids
    /// make theirs accessible, which in their case means their IntelCoverage
    /// is &gt; Basic.</remarks>
    /// </summary>
    /// <param name="player">The player whose InfoAccess to this Item changed.</param>
    protected virtual void HandleInfoAccessChangedFor(Player player) { }

    /// <summary>
    /// Handles a change in the User's IntelCoverage of this item.
    /// </summary>
    protected virtual void HandleUserIntelCoverageChanged() {
        AssessIsDiscernibleToUser();
        if (IsHoveredHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHoveredHud(true);
        }
        DisplayMgr.IsDisplayEnabled = UserIntelCoverage != IntelCoverage.None;
    }

    #region Cleanup

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (Data != null) { // Data can be null for a time when creators delay builds
            Data.intelCoverageChanged -= IntelCoverageChangedEventHandler;
        }
    }

    #endregion

    #region Debug

    public bool __IsPlayerEntitledToComprehensiveRelationship(Player player) {
        return Data.__IsPlayerEntitledToComprehensiveRelationship(player);
    }

    #endregion

    #region IDetectionHandlerClient Members

    /// <summary>
    /// Attempts to change the IntelCoverage of this Item to newCoverage. Returns <c>true</c> if the coverage
    /// was changed, false if not. In both cases, resultingCoverage will reflect the currentCoverage, changed or not.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    /// <param name="resultingCoverage">The resulting coverage.</param>
    /// <returns></returns>
    bool IDetectionHandlerClient.TryChangeIntelCoverage(Player player, IntelCoverage newCoverage, out IntelCoverage resultingCoverage) {
        return Data.TryChangeIntelCoverage(player, newCoverage, out resultingCoverage);
    }

    #endregion



}

