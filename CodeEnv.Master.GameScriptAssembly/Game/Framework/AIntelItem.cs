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

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        Data.onUserIntelCoverageChanged += OnUserIntelCoverageChanged;
    }

    #endregion

    #region Model Methods

    #endregion

    #region View Methods

    public IntelCoverage GetUserIntelCoverage() { return Data.GetUserIntelCoverage(); }

    public IntelCoverage GetIntelCoverage(Player player) { return Data.GetIntelCoverage(player); }

    public bool SetIntelCoverage(Player player, IntelCoverage coverage) {
        return Data.SetIntelCoverage(player, coverage);
    }

    protected virtual void OnUserIntelCoverageChanged() {
        //D.Log("{0}.OnUserIntelCoverageChanged() called. IntelCoverage = {1}.", FullName, GetUserIntelCoverage().GetName());
        AssessIsDiscernibleToUser();
        if (IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
        }
        var toEnableDisplayMgr = Data.GetUserIntelCoverage() != IntelCoverage.None;
        DisplayMgr.EnableDisplay(toEnableDisplayMgr);
    }

    protected override void AssessIsDiscernibleToUser() {
        var isInMainCameraLOS = DisplayMgr == null ? true : DisplayMgr.IsInMainCameraLOS;
        IsDiscernibleToUser = isInMainCameraLOS && Data.GetUserIntelCoverage() != IntelCoverage.None;
    }

    #endregion

    #region Events

    #endregion

    #region Cleanup

    protected override void Unsubscribe() {
        base.Unsubscribe();
        if (Data != null) { // Data can be null for a time when creators delay builds
            Data.onUserIntelCoverageChanged -= OnUserIntelCoverageChanged;
        }
    }

    #endregion
}

