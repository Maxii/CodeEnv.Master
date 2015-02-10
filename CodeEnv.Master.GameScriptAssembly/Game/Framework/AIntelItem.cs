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
public abstract class AIntelItem : ADiscernibleItem {

    public new AIntelItemData Data {
        get { return base.Data as AIntelItemData; }
        set { base.Data = value; }
    }

    public IntelCoverage HumanPlayerIntelCoverage {
        get { return Data.HumanPlayerIntelCoverage; }
        set { Data.HumanPlayerIntelCoverage = value; }
    }

    #region Initialization

    protected override void SubscribeToDataValueChanges() {
        base.SubscribeToDataValueChanges();
        _subscribers.Add(Data.HumanPlayerIntel.SubscribeToPropertyChanged<AIntel, IntelCoverage>(hpi => hpi.CurrentCoverage, OnHumanPlayerIntelCoverageChanged));
    }

    #endregion

    #region Model Methods

    #endregion

    #region View Methods

    public void SetIntelCoverage(Player player, IntelCoverage coverage) {
        Data.SetIntelCoverage(player, coverage);
    }

    public IntelCoverage GetIntelCoverage(Player player) {
        return Data.GetIntelCoverage(player);
    }

    protected virtual void OnHumanPlayerIntelCoverageChanged() {
        AssessDiscernability();
        if (IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
        }
    }

    public override void AssessDiscernability() {
        IsDiscernible = InCameraLOS && HumanPlayerIntelCoverage != IntelCoverage.None;
    }

    #endregion

    #region Mouse Events

    #endregion

}

