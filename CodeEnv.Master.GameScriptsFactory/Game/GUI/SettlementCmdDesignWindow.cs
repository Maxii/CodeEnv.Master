﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdDesignWindow.cs
// GuiWindow for designing SettlementCmds, managed by the DesignScreensManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiWindow for designing SettlementCmds, managed by the DesignScreensManager.
/// </summary>
public class SettlementCmdDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitMemberDesign newDesign) {
        _gameMgr.PlayersDesigns.Add(newDesign as SettlementCmdDesign);
    }

    protected override AUnitMemberDesign CopyDesignFrom(AUnitMemberDesign design) {
        return new SettlementCmdDesign(design as SettlementCmdDesign);
    }

    protected override bool TryGet3DModelFor(AUnitMemberDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        modelDimensions = default(Vector3);
        modelPrefab = null;
        return false;
    }

    protected override IEnumerable<AUnitMemberDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return _gameMgr.PlayersDesigns.GetAllUserSettlementCmdDesigns(includeObsolete).Cast<AUnitMemberDesign>();
    }

    protected override bool IsDesignContentEqual(AUnitMemberDesign previousDesign, AUnitMemberDesign newDesign) {
        return GameUtility.IsDesignContentEqual(previousDesign as SettlementCmdDesign, newDesign as SettlementCmdDesign);
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = null;
        popupContent = null;
        return false;
    }

    protected override AUnitMemberDesign GetEmptyTemplateDesign(string designNameHint) {
        D.AssertNull(designNameHint);
        return _gameMgr.PlayersDesigns.GetUserSettlementCmdDesign(TempGameValues.EmptySettlementCmdTemplateDesignName);
    }

    protected override void ObsoleteDesign(string designName) {
        _gameMgr.PlayersDesigns.ObsoleteUserSettlementCmdDesign(designName);
    }

    protected override IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.GetAvailableUserEquipmentStats(AUnitCmdDesign.SupportedEquipCategories);
    }

}

