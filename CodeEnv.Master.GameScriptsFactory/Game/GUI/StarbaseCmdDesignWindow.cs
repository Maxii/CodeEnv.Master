// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdDesignWindow.cs
// GuiWindow for designing StarbaseCmds, managed by the DesignScreensManager.
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
/// GuiWindow for designing StarbaseCmds, managed by the DesignScreensManager.
/// </summary>
public class StarbaseCmdDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitDesign newDesign) {
        _gameMgr.PlayersDesigns.Add(newDesign as StarbaseCmdDesign);
    }

    protected override AUnitDesign CopyDesignFrom(AUnitDesign design) {
        return new StarbaseCmdDesign(design as StarbaseCmdDesign);
    }

    protected override bool TryGet3DModelFor(AUnitDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        modelDimensions = default(Vector3);
        modelPrefab = null;
        return false;
    }

    protected override IEnumerable<AUnitDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return _gameMgr.PlayersDesigns.GetAllUserStarbaseCmdDesigns(includeObsolete).Cast<AUnitDesign>();
    }

    protected override bool IsDesignContentEqual(AUnitDesign previousDesign, AUnitDesign newDesign) {
        return GameUtility.IsDesignContentEqual(previousDesign as StarbaseCmdDesign, newDesign as StarbaseCmdDesign);
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = null;
        popupContent = null;
        return false;
    }

    protected override AUnitDesign GetEmptyTemplateDesign(string designNameHint) {
        D.AssertNull(designNameHint);
        return _gameMgr.PlayersDesigns.GetUserStarbaseCmdDesign(TempGameValues.EmptyStarbaseCmdTemplateDesignName);
    }

    protected override void ObsoleteDesign(string designName) {
        _gameMgr.PlayersDesigns.ObsoleteUserStarbaseCmdDesign(designName);
    }

    protected override IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.GetAvailableUserEquipmentStats(ACommandDesign.SupportedEquipCategories);
    }

}

