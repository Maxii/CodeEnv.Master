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
public abstract class AIntelItem : ADiscernibleItem, IIntelItem {

    public new AIntelItemData Data {
        get { return base.Data as AIntelItemData; }
        set { base.Data = value; }
    }

    #region Initialization

    protected override void InitializeOnData() {
        Data.InitializePlayersIntel();  // moved here to move out of Data constructor
    }

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        Data.userIntelCoverageChanged += UserIntelCoverageChangedEventHandler;
    }

    #endregion

    public IntelCoverage GetUserIntelCoverage() { return Data.GetIntelCoverage(_gameMgr.UserPlayer); }

    public IntelCoverage GetIntelCoverage(Player player) { return Data.GetIntelCoverage(player); }

    public bool SetIntelCoverage(Player player, IntelCoverage coverage) {
        return Data.SetIntelCoverage(player, coverage);
    }

    protected override void AssessIsDiscernibleToUser() {
        var isInMainCameraLOS = DisplayMgr == null ? true : DisplayMgr.IsInMainCameraLOS;
        IsDiscernibleToUser = isInMainCameraLOS && GetUserIntelCoverage() != IntelCoverage.None;
    }


    #region Event and Property Change Handlers

    protected virtual void UserIntelCoverageChangedEventHandler(object sender, EventArgs e) {
        D.Log("{0}.UserIntelCoverageChangedHandler() called. IntelCoverage = {1}.", FullName, GetUserIntelCoverage().GetValueName());
        AssessIsDiscernibleToUser();
        if (IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
        }
        var toEnableDisplayMgr = GetUserIntelCoverage() != IntelCoverage.None;
        DisplayMgr.EnableDisplay(toEnableDisplayMgr);
    }

    #endregion

    #region Cleanup

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (Data != null) { // Data can be null for a time when creators delay builds
            Data.userIntelCoverageChanged -= UserIntelCoverageChangedEventHandler;
        }
    }

    #endregion

}

