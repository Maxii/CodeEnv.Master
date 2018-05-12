// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesignWindow.cs
// GuiWindow used by the User to design Ships, managed by the DesignScreensManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiWindow used by the User to design Ships, managed by the DesignScreensManager.
/// </summary>
public class ShipDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitMemberDesign newDesign) {
        UserDesigns.Add(newDesign as ShipDesign);
    }

    protected override AUnitMemberDesign CopyDesignFrom(AUnitMemberDesign design) {
        return new ShipDesign(design as ShipDesign);
    }

    protected override bool TryGet3DModelFor(AUnitMemberDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        ShipDesign sDesign = design as ShipDesign;
        modelDimensions = sDesign.HullCategory.Dimensions();
        modelPrefab = RequiredPrefabs.Instance.shipHulls.Single(hull => hull.HullCategory == sDesign.HullCategory).gameObject;
        return true;
    }

    protected override IEnumerable<AUnitMemberDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return UserDesigns.GetAllDeployableShipDesigns(includeObsolete, includeDefault: false).Cast<AUnitMemberDesign>();
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = "Hull";
        popupContent = TempGameValues.ShipHullCategoriesInUse.Select(hullCat => hullCat.GetValueName()).ToList();
        return true;
    }

    protected override AUnitMemberDesign GetEmptyTemplateDesign(string emptyTemplateHint) {
        D.AssertNotNull(emptyTemplateHint);
        ShipHullCategory designHullCat = Enums<ShipHullCategory>.Parse(emptyTemplateHint);
        return UserDesigns.GetCurrentShipTemplateDesign(designHullCat);
    }

    protected override void Obsolete(AUnitMemberDesign design) {
        UserDesigns.ObsoleteDesign(design as ShipDesign);
    }

    protected override IEnumerable<AEquipmentStat> GetUserCurrentEquipmentStatsAvailableFor(AUnitMemberDesign pickedDesign) {
        return UserDesigns.GetCurrentShipOptEquipStats();
    }
}

