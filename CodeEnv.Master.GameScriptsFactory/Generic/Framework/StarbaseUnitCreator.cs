// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseUnitCreator.cs
// Initialization class that deploys a Starbase at the location of this StarbaseCreator.
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
/// Initialization class that deploys a Starbase at the location of this StarbaseCreator. 
/// </summary>
public class StarbaseUnitCreator : AUnitCreator<FacilityItem, FacilityHullCategory, FacilityData, FacilityHullStat, StarbaseCmdItem> {

    public DebugBaseFormation formation = DebugBaseFormation.Random;

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityHullStat CreateElementHullStat(FacilityHullCategory hullCat, string elementName) {
        float science = hullCat == FacilityHullCategory.Laboratory ? 10F : Constants.ZeroF;
        float culture = hullCat == FacilityHullCategory.CentralHub || hullCat == FacilityHullCategory.ColonyHab ? 2.5F : Constants.ZeroF;
        float income = __GetIncome(hullCat);
        float expense = __GetExpense(hullCat);
        float hullMass = __GetHullMass(hullCat);
        Vector3 hullDimensions = __GetHullDimensions(hullCat);
        return new FacilityHullStat(hullCat, elementName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), hullDimensions, science, culture, income);
    }

    protected override void MakeAndRecordDesign(string designName, FacilityHullStat hullStat, IEnumerable<AWeaponStat> weaponStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats) {
        FacilityHullCategory hullCategory = hullStat.HullCategory;
        var weaponDesigns = _factory.__MakeWeaponDesigns(hullCategory, weaponStats);
        var design = new FacilityDesign(_owner, designName, hullStat, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats);
        GameManager.Instance.PlayersDesigns.Add(design);
    }

    protected override FacilityItem MakeElement(string designName) {
        FacilityDesign design = GameManager.Instance.PlayersDesigns.GetFacilityDesign(_owner, designName);
        CameraFollowableStat cameraStat = __MakeElementCameraStat(design.HullStat);
        return _factory.MakeInstance(_owner, Topography.OpenSpace, cameraStat, design);
    }

    protected override void PopulateElement(string designName, ref FacilityItem element) {
        FacilityDesign design = GameManager.Instance.PlayersDesigns.GetFacilityDesign(_owner, designName);
        var cameraStat = __MakeElementCameraStat(design.HullStat);
        _factory.PopulateInstance(_owner, Topography.OpenSpace, cameraStat, design, ref element);
    }

    protected override FacilityHullCategory GetCategory(FacilityHullStat hullStat) { return hullStat.HullCategory; }

    protected override FacilityHullCategory GetCategory(AHull hull) { return (hull as FacilityHull).HullCategory; }

    protected override FacilityHullCategory[] ElementCategories {
        get {
            return new FacilityHullCategory[] { FacilityHullCategory.Factory, FacilityHullCategory.Defense, FacilityHullCategory.Economic,
                         FacilityHullCategory.Laboratory, FacilityHullCategory.Barracks, FacilityHullCategory.ColonyHab };
        }
    }

    protected override FacilityHullCategory[] HQElementCategories {
        get { return new FacilityHullCategory[] { FacilityHullCategory.CentralHub }; }
    }

    protected override StarbaseCmdItem MakeCommand(Player owner) {
        LogEvent();
        var countermeasures = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        UnitCmdStat cmdStat = __MakeCmdStat();
        CameraUnitCmdStat cameraStat = __MakeCmdCameraStat(TempGameValues.FacilityMaxRadius);
        StarbaseCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSingleComponentInChildren<StarbaseCmdItem>();
            _factory.PopulateInstance(cmdStat, cameraStat, countermeasures, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, cameraStat, countermeasures, owner, gameObject);
        }
        cmd.IsTrackingLabelEnabled = enableTrackingLabel;
        return cmd;
    }

    protected override bool DeployUnit() {
        LogEvent();
        // Starbases don't need to be deployed. They are already on location
        PathfindingManager.Instance.Graph.UpdateGraph(_command);
        return true;
    }

    protected override void AssignHQElement() {
        // 4.19.16: TEMP: Like Fleets, Bases can temporarily pick another facility if there is no facility present that matches
        // an 'acceptable' HQELementCategory, aka CentralHub. Bases used to require a CentralHub as their HQ, but with the
        // demise (sometime over a year ago) of Bases distributing damage to protect the HQ, there must be an alternative if a 
        // CentralHub is not present in the preset Base.
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => HQElementCategories.Contains((e as FacilityItem).Data.HullCategory));
        if (candidateHQElements.IsNullOrEmpty()) {
            // _command might not hold a valid HQ Element if preset
            D.Warn("No valid HQElements for {0} found.", UnitName);
            candidateHQElements = _command.Elements;
        }
        var hqElement = RandomExtended.Choice(candidateHQElements) as FacilityItem;
        _command.HQElement = hqElement;
    }

    protected override void __IssueFirstUnitOrder(Action onCompleted) {
        LogEvent();
        onCompleted();
    }

    protected override int GetMaxLosWeaponsAllowed(FacilityHullCategory hullCategory) {
        return hullCategory.__MaxLOSWeapons();
    }

    protected override int GetMaxMissileWeaponsAllowed(FacilityHullCategory hullCategory) {
        return hullCategory.__MaxMissileWeapons();
    }

    private Formation __ConvertFormation(DebugBaseFormation debugFormation) {
        switch (debugFormation) {
            case DebugBaseFormation.Globe:
                return Formation.Globe;
            case DebugBaseFormation.Diamond:
                return Formation.Diamond;
            case DebugBaseFormation.Plane:
                return Formation.Plane;
            case DebugBaseFormation.Spread:
                return Formation.Spread;
            case DebugBaseFormation.Random:
                return Enums<Formation>.GetRandomExcept(default(Formation), Formation.Wedge);
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(debugFormation));
        }
    }

    private UnitCmdStat __MakeCmdStat() {
        float maxHitPts = 10F;
        float maxCmdEffect = 1.0F;
        Formation chosenFormation = __ConvertFormation(formation);
        return new UnitCmdStat(UnitName, maxHitPts, maxCmdEffect, chosenFormation);
    }

    private CameraUnitCmdStat __MakeCmdCameraStat(float maxElementRadius) {
        float minViewDistance = maxElementRadius + 1F; // close to the HQ Facility
        float optViewDistanceAdder = Constants.ZeroF;
        return new CameraUnitCmdStat(minViewDistance, optViewDistanceAdder, fov: 60F);
    }

    private CameraFollowableStat __MakeElementCameraStat(FacilityHullStat hullStat) {
        FacilityHullCategory hullCat = hullStat.HullCategory;
        float fov;
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Defense:
                fov = 70F;
                break;
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Laboratory:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Barracks:
                fov = 60F;
                break;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = hullStat.HullDimensions.magnitude / 2F;
        //D.Log("Radius of {0} is {1:0.##}.", hullCat.GetValueName(), radius);
        float minViewDistance = radius * 2F;
        float optViewDistance = radius * 3F;
        return new CameraFollowableStat(minViewDistance, optViewDistance, fov);
    }

    private float __GetIncome(FacilityHullCategory hullCat) {
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
                return 5F;
            case FacilityHullCategory.Economic:
                return 20F;
            case FacilityHullCategory.Barracks:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Defense:
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Laboratory:
                return Constants.ZeroF;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
    }

    private float __GetExpense(FacilityHullCategory hullCat) {
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Economic:
                return Constants.ZeroF;
            case FacilityHullCategory.Barracks:
            case FacilityHullCategory.ColonyHab:
                return 3F;
            case FacilityHullCategory.Defense:
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Laboratory:
                return 5F;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
    }

    private float __GetHullMass(FacilityHullCategory hullCat) {
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
                return 10000F;
            case FacilityHullCategory.Defense:
            case FacilityHullCategory.Factory:
                return 5000F;
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Barracks:
            case FacilityHullCategory.Laboratory:
                return 2000F;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
    }

    /// <summary>
    /// The dimensions of the facility with this Category. 
    /// </summary>
    /// <param name="hullCat">The category.</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    private Vector3 __GetHullDimensions(FacilityHullCategory hullCat) {
        Vector3 dimensions;
        switch (hullCat) {
            case FacilityHullCategory.CentralHub:
            case FacilityHullCategory.Defense:
                dimensions = new Vector3(.4F, .4F, .4F);
                break;
            case FacilityHullCategory.Economic:
            case FacilityHullCategory.Factory:
            case FacilityHullCategory.Laboratory:
            case FacilityHullCategory.ColonyHab:
            case FacilityHullCategory.Barracks:
                dimensions = new Vector3(.2F, .2F, .2F);
                break;
            case FacilityHullCategory.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        }
        float radius = dimensions.magnitude / 2F;
        D.Warn(radius > TempGameValues.FacilityMaxRadius, "Facility {0}.Radius {1:0.####} > MaxRadius {2:0.##}.", hullCat.GetValueName(), radius, TempGameValues.FacilityMaxRadius);
        return dimensions;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

