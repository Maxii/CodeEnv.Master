﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipDesignWindow.cs
// GuiWindow for designing Ships, managed by the DesignScreensManager.
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
/// GuiWindow for designing Ships, managed by the DesignScreensManager.
/// </summary>
public class ShipDesignWindow : AUnitDesignWindow {

    protected override void AddToPlayerDesigns(AUnitDesign newDesign) {
        _gameMgr.PlayersDesigns.Add(newDesign as ShipDesign);
    }

    protected override AUnitDesign CopyDesignFrom(AUnitDesign design) {
        return new ShipDesign(design as ShipDesign);
    }

    protected override bool TryGet3DModelFor(AUnitDesign design, out Vector3 modelDimensions, out GameObject modelPrefab) {
        ShipDesign sDesign = design as ShipDesign;
        modelDimensions = sDesign.HullCategory.Dimensions();
        modelPrefab = RequiredPrefabs.Instance.shipHulls.Single(hull => hull.HullCategory == sDesign.HullCategory).gameObject;
        return true;
    }

    protected override IEnumerable<AUnitDesign> GetRegisteredUserDesigns(bool includeObsolete) {
        return _gameMgr.PlayersDesigns.GetAllUserShipDesigns(includeObsolete).Cast<AUnitDesign>();
    }

    protected override bool IsDesignContentEqual(AUnitDesign previousDesign, AUnitDesign newDesign) {
        return GameUtility.IsDesignContentEqual(previousDesign as ShipDesign, newDesign as ShipDesign);
    }

    protected override bool TryGetCreateDesignPopupContent(out string popupTitle, out List<string> popupContent) {
        popupTitle = "Hull";
        popupContent = TempGameValues.ShipHullCategoriesInUse.Select(hullCat => hullCat.GetValueName()).ToList();
        return true;
    }

    protected override AUnitDesign GetEmptyTemplateDesign(string designNameHint) {
        D.AssertNotNull(designNameHint);
        ShipHullCategory designHullCat = Enums<ShipHullCategory>.Parse(designNameHint);
        return _gameMgr.PlayersDesigns.GetUserShipDesign(designHullCat.GetEmptyTemplateDesignName());
    }

    protected override void ObsoleteDesign(string designName) {
        _gameMgr.PlayersDesigns.ObsoleteUserShipDesign(designName);
    }

    protected override IEnumerable<AEquipmentStat> GetAvailableUserEquipmentStats() {
        return _gameMgr.UniverseCreator.UnitConfigurator.GetAvailableUserEquipmentStats(AElementDesign.SupportedEquipCategories);
    }
}

