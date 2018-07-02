﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityDesignWindow.cs
// GuiWindow used by the User to design Facilities, managed by the DesignScreensManager.
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
/// GuiWindow used by the User to design Facilities, managed by the DesignScreensManager.
/// </summary>
public class FacilityDesignWindow : AUnitDesignWindow {

    private UserResearchManager _userRschMgr;
    private UserResearchManager UserRschMgr {
        get {
            _userRschMgr = _userRschMgr ?? _gameMgr.UserAIManager.ResearchMgr;
            return _userRschMgr;
        }
    }

    protected override void AddToPlayerDesigns(AUnitMemberDesign newDesign) {
        UserDesigns.Add(newDesign as FacilityDesign);
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
        return UserDesigns.GetAllDeployableFacilityDesigns(includeObsolete, includeDefault: false).Cast<AUnitMemberDesign>();
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = "Hull";
        popupContent = UserRschMgr.GetDiscoveredFacilityHullCats().Select(hullCat => hullCat.GetValueName()).ToList();
        return true;
    }

    protected override AUnitMemberDesign GetEmptyTemplateDesign(string emptyTemplateHint) {
        D.AssertNotNull(emptyTemplateHint);
        FacilityHullCategory designHullCat = Enums<FacilityHullCategory>.Parse(emptyTemplateHint);
        return UserDesigns.GetCurrentFacilityTemplateDesign(designHullCat);
    }

    protected override void Obsolete(AUnitMemberDesign design) {
        UserDesigns.ObsoleteDesign(design as FacilityDesign);
    }

    protected override IEnumerable<AEquipmentStat> GetUserCurrentEquipmentStatsAvailableFor(AUnitMemberDesign pickedDesign) {
        return UserDesigns.GetCurrentFacilityOptEquipStats();
    }

}

