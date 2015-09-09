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
public class StarbaseUnitCreator : AUnitCreator<FacilityItem, FacilityCategory, FacilityData, FacilityHullStat, StarbaseCmdItem> {

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityHullStat CreateElementHullStat(FacilityCategory hullCat, string elementName) {
        float science = hullCat == FacilityCategory.Laboratory ? 10F : Constants.ZeroF;
        float culture = hullCat == FacilityCategory.CentralHub || hullCat == FacilityCategory.Colonizer ? 2.5F : Constants.ZeroF;
        float income = __GetIncome(hullCat);
        float expense = __GetExpense(hullCat);
        float hullMass = TempGameValues.__GetHullMass(hullCat);
        return new FacilityHullStat(hullCat, elementName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F,
            hullMass, 0F, expense, 50F, new DamageStrength(2F, 2F, 2F), science, culture, income);
    }

    protected override FacilityItem MakeElement(FacilityHullStat hullStat, IEnumerable<WeaponStat> wStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
    IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats) {
        return _factory.MakeInstance(hullStat, Topography.OpenSpace, _owner, wStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats);
    }

    protected override void PopulateElement(FacilityHullStat hullStat, IEnumerable<WeaponStat> wStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
    IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats, ref FacilityItem element) {
        _factory.PopulateInstance(hullStat, Topography.OpenSpace, _owner, wStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats, ref element);
    }

    protected override FacilityCategory GetCategory(FacilityHullStat hullStat) {
        return hullStat.Category;
    }

    protected override FacilityCategory GetCategory(FacilityItem element) {
        return element.category;
    }

    protected override FacilityCategory[] ElementCategories {
        get {
            return new FacilityCategory[] { FacilityCategory.Factory, FacilityCategory.Defense, FacilityCategory.Economic, 
                         FacilityCategory.Laboratory, FacilityCategory.Barracks, FacilityCategory.Colonizer };
        }
    }

    protected override FacilityCategory[] HQElementCategories {
        get { return new FacilityCategory[] { FacilityCategory.CentralHub }; }
    }

    protected override StarbaseCmdItem MakeCommand(Player owner) {
        LogEvent();
        var countermeasures = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        StarbaseCmdStat cmdStat = new StarbaseCmdStat(UnitName, 10F, 100, Formation.Circle);

        StarbaseCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeFirstMonoBehaviourInChildren<StarbaseCmdItem>();
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
        var candidateHQElements = _command.Elements.Where(e => HQElementCategories.Contains((e as FacilityItem).Data.Category));
        D.Assert(!candidateHQElements.IsNullOrEmpty()); // bases must have a CentralHub, even if preset
        _command.HQElement = RandomExtended.Choice(candidateHQElements) as FacilityItem;
    }

    protected override void __IssueFirstUnitCommand() {
        LogEvent();
    }

    private float __GetIncome(FacilityCategory category) {
        switch (category) {
            case FacilityCategory.CentralHub:
                return 5F;
            case FacilityCategory.Economic:
                return 20F;
            case FacilityCategory.Barracks:
            case FacilityCategory.Colonizer:
            case FacilityCategory.Defense:
            case FacilityCategory.Factory:
            case FacilityCategory.Laboratory:
                return Constants.ZeroF;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
        }
    }

    private float __GetExpense(FacilityCategory category) {
        switch (category) {
            case FacilityCategory.CentralHub:
            case FacilityCategory.Economic:
                return Constants.ZeroF;
            case FacilityCategory.Barracks:
            case FacilityCategory.Colonizer:
                return 3F;
            case FacilityCategory.Defense:
            case FacilityCategory.Factory:
            case FacilityCategory.Laboratory:
                return 5F;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

