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

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityHullStat CreateElementHullStat(FacilityHullCategory hullCat, string elementName) {
        float science = hullCat == FacilityHullCategory.Laboratory ? 10F : Constants.ZeroF;
        float culture = hullCat == FacilityHullCategory.CentralHub || hullCat == FacilityHullCategory.ColonyHab ? 2.5F : Constants.ZeroF;
        float income = __GetIncome(hullCat);
        float expense = __GetExpense(hullCat);
        float hullMass = __GetHullMass(hullCat);
        return new FacilityHullStat(hullCat, elementName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), science, culture, income);
    }

    protected override void MakeAndRecordDesign(string designName, FacilityHullStat hullStat, IEnumerable<AWeaponStat> weaponStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats) {
        FacilityHullCategory hullCategory = hullStat.HullCategory;
        var weaponDesigns = _factory.__MakeWeaponDesigns(hullCategory, weaponStats);
        var design = new FacilityDesign(_owner, designName, hullStat, weaponDesigns, passiveCmStats, activeCmStats, sensorStats, shieldGenStats);
        GameManager.Instance.PlayersDesigns.Add(design);
    }

    protected override FacilityItem MakeElement(string designName) {
        return _factory.MakeFacilityInstance(_owner, Topography.OpenSpace, designName);
    }

    protected override void PopulateElement(string designName, ref FacilityItem element) {
        _factory.PopulateInstance(_owner, Topography.OpenSpace, designName, ref element);
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
        StarbaseCmdStat cmdStat = new StarbaseCmdStat(UnitName, 10F, 100, Formation.Circle);

        StarbaseCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSingleComponentInChildren<StarbaseCmdItem>();
            _factory.PopulateInstance(cmdStat, countermeasures, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, countermeasures, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        cmd.IsTrackingLabelEnabled = enableTrackingLabel;
        return cmd;
    }

    protected override bool DeployUnit() {
        LogEvent();
        // Starbases don't need to be deployed. They are already on location
        //PathfindingManager.Instance.Graph.UpdateGraph(_command);  // TODO Not yet implemented
        return true;
    }

    protected override void AssignHQElement() {
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => HQElementCategories.Contains((e as FacilityItem).Data.HullCategory));
        D.Assert(!candidateHQElements.IsNullOrEmpty()); // bases must have a CentralHub, even if preset
        _command.HQElement = RandomExtended.Choice(candidateHQElements) as FacilityItem;
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

    private float __GetIncome(FacilityHullCategory category) {
        switch (category) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
        }
    }

    private float __GetExpense(FacilityHullCategory category) {
        switch (category) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

