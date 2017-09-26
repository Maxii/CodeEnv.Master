// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDesignWindow.cs
// GuiWindow for designing Facilities, managed by the DesignScreensManager.
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
/// GuiWindow for designing Facilities, managed by the DesignScreensManager.
/// </summary>
public class FacilityDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitMemberDesign newDesign) {
        _gameMgr.PlayersDesigns.Add(newDesign as FacilityDesign);
    }

    protected override AUnitMemberDesign CopyDesignFrom(AUnitMemberDesign design) {
        return new FacilityDesign(design as FacilityDesign);
    }

    protected override bool TryGet3DModelFor(AUnitMemberDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        FacilityDesign fDesign = design as FacilityDesign;
        modelDimensions = fDesign.HullCategory.Dimensions();
        modelPrefab = RequiredPrefabs.Instance.facilityHulls.Single(hull => hull.HullCategory == fDesign.HullCategory).gameObject;
        return true;
    }

    protected override IEnumerable<AUnitMemberDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return _gameMgr.PlayersDesigns.GetAllUserFacilityDesigns(includeObsolete).Cast<AUnitMemberDesign>();
    }

    protected override bool IsDesignContentEqual(AUnitMemberDesign previousDesign, AUnitMemberDesign newDesign) {
        return GameUtility.IsDesignContentEqual(previousDesign as FacilityDesign, newDesign as FacilityDesign);
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = "Hull";
        popupContent = TempGameValues.FacilityHullCategoriesInUse.Select(hullCat => hullCat.GetValueName()).ToList();
        return true;
    }

    protected override AUnitMemberDesign GetEmptyTemplateDesign(string designNameHint) {
        D.AssertNotNull(designNameHint);
        FacilityHullCategory designHullCat = Enums<FacilityHullCategory>.Parse(designNameHint);
        return _gameMgr.PlayersDesigns.GetUserFacilityDesign(designHullCat.GetEmptyTemplateDesignName());
    }

    protected override void ObsoleteDesign(string designName) {
        _gameMgr.PlayersDesigns.ObsoleteUserFacilityDesign(designName);
    }

    protected override IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.GetAvailableUserEquipmentStats(AUnitElementDesign.SupportedEquipCategories);
    }

}

