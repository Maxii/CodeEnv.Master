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

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract class for ADiscernibleItem's that have knowledge of each player's IntelCoverage.
/// </summary>
public abstract class AIntelItem : ADiscernibleItem, IIntelItem, IIntelItem_Ltd {

    public IntelCoverage UserIntelCoverage { get { return Data.GetIntelCoverage(_gameMgr.UserPlayer); } }

    public new AIntelItemData Data {
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
    /// Sets the Intel coverage for this player. Returns <c>true</c> if the <c>newCoverage</c>
    /// was successfully applied, and <c>false</c> if it was rejected due to the inability of
    /// the item to regress its IntelCoverage.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="newCoverage">The new coverage.</param>
    /// <returns></returns>
    public bool SetIntelCoverage(Player player, IntelCoverage newCoverage) {
        return Data.SetIntelCoverage(player, newCoverage);
    }

    protected override void AssessIsDiscernibleToUser() {
        var isInMainCameraLOS = DisplayMgr != null ? DisplayMgr.IsInMainCameraLOS : true;
        IsDiscernibleToUser = isInMainCameraLOS && UserIntelCoverage > IntelCoverage.None;
    }

    #region Event and Property Change Handlers

    private void IntelCoverageChangedEventHandler(object sender, AIntelItemData.IntelCoverageChangedEventArgs e) {
        if (!IsOperational) {
            // can be called before CommenceOperations if DebugSettings.AllIntelCoverageComprehensive = true
            return;
        }
        Player playerWhosCoverageChgd = e.Player;
        D.Log(ShowDebugLog, "{0}.IntelCoverageChangedHandler() called. {1}'s new IntelCoverage = {2}.", FullName, playerWhosCoverageChgd.Name, GetIntelCoverage(playerWhosCoverageChgd));
        if (playerWhosCoverageChgd == _gameMgr.UserPlayer) {
            HandleUserIntelCoverageChanged();
        }

        Player playerWhosInfoAccessChgd = playerWhosCoverageChgd;
        OnInfoAccessChanged(playerWhosInfoAccessChgd);
    }

    #endregion

    /// <summary>
    /// Handles a change in the User's IntelCoverage of this item.
    /// </summary>
    protected virtual void HandleUserIntelCoverageChanged() {
        AssessIsDiscernibleToUser();
        if (IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
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

}

