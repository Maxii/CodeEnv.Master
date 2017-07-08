// --------------------------------------------------------------------------------------------------------------------
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

    protected override void AddToPlayerDesigns(AUnitDesign newDesign) {
        _gameMgr.PlayersDesigns.Add(newDesign as SettlementCmdDesign);
    }

    protected override AUnitDesign CopyDesignFrom(AUnitDesign design) {
        return new SettlementCmdDesign(design as SettlementCmdDesign);
    }

    protected override bool TryGet3DModelFor(AUnitDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        modelDimensions = default(Vector3);
        modelPrefab = null;
        return false;
    }

    protected override IEnumerable<AUnitDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return _gameMgr.PlayersDesigns.GetAllUserSettlementCmdDesigns(includeObsolete).Cast<AUnitDesign>();
    }

    protected override bool IsDesignContentEqual(AUnitDesign previousDesign, AUnitDesign newDesign) {
        return GameUtility.IsDesignContentEqual(previousDesign as SettlementCmdDesign, newDesign as SettlementCmdDesign);
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = null;
        popupContent = null;
        return false;
    }

    protected override AUnitDesign GetEmptyTemplateDesign(string designNameHint) {
        D.AssertNull(designNameHint);
        return _gameMgr.PlayersDesigns.GetUserSettlementCmdDesign(TempGameValues.EmptySettlementCmdTemplateDesignName);
    }

    protected override void ObsoleteDesign(string designName) {
        _gameMgr.PlayersDesigns.ObsoleteUserSettlementCmdDesign(designName);
    }

    protected override IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.GetAvailableUserEquipmentStats(ACommandDesign.SupportedEquipCategories);
    }

}

