// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItem.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public abstract class AIntelItem : AItem {

    public new AIntelData2 Data {
        get { return base.Data as AIntelData2; }
        set { base.Data = value; }
    }

    public IntelCoverage HumanPlayerIntelCoverage {
        get { return Data.HumanPlayerIntelCoverage; }
        set { Data.HumanPlayerIntelCoverage = value; }
    }


    #region Initialization

    /// <summary>
    /// Subscribes to changes to values contained in Data. 
    /// </summary>
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

    #region Cleanup

    #endregion


}

