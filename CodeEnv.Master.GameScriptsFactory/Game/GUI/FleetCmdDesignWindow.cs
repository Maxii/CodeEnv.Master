// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdDesignWindow.cs
// GuiWindow for designing FleetCmds, managed by the DesignScreensManager.
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
/// GuiWindow for designing FleetCmds, managed by the DesignScreensManager.
/// </summary>
public class FleetCmdDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitDesign newDesign) {
        _gameMgr.PlayersDesigns.Add(newDesign as FleetCmdDesign);
    }

    protected override AUnitDesign CopyDesignFrom(AUnitDesign design) {
        return new FleetCmdDesign(design as FleetCmdDesign);
    }

    protected override bool TryGet3DModelFor(AUnitDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        modelDimensions = default(Vector3);
        modelPrefab = null;
        return false;
    }

    protected override IEnumerable<AUnitDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return _gameMgr.PlayersDesigns.GetAllUserFleetCmdDesigns(includeObsolete).Cast<AUnitDesign>();
    }

    protected override bool IsDesignContentEqual(AUnitDesign previousDesign, AUnitDesign newDesign) {
        return GameUtility.IsDesignContentEqual(previousDesign as FleetCmdDesign, newDesign as FleetCmdDesign);
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = null;
        popupContent = null;
        return false;
    }

    protected override AUnitDesign GetEmptyTemplateDesign(string designNameHint) {
        D.AssertNull(designNameHint);
        return _gameMgr.PlayersDesigns.GetUserFleetCmdDesign(TempGameValues.EmptyFleetCmdTemplateDesignName);
    }

    protected override void ObsoleteDesign(string designName) {
        _gameMgr.PlayersDesigns.ObsoleteUserFleetCmdDesign(designName);
    }

    protected override IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.GetAvailableUserEquipmentStats(ACommandDesign.SupportedEquipCategories);
    }

}

