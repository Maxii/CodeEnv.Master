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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initialization class that deploys a Starbase at the location of this StarbaseCreator. 
/// </summary>
public class StarbaseUnitCreator : AUnitCreator<FacilityItem, FacilityCategory, FacilityData, FacilityStat, StarbaseCmdItem> {

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStat CreateElementStat(FacilityCategory category, string elementName) {
        return new FacilityStat(elementName, 10000F, 50F, category);
    }

    protected override FacilityItem MakeElement(FacilityStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats) {
        return _factory.MakeInstance(stat, Topography.OpenSpace, wStats, cmStats, sensorStats, _owner);
    }

    protected override void PopulateElement(FacilityStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, ref FacilityItem element) {
        _factory.PopulateInstance(stat, Topography.OpenSpace, wStats, cmStats, sensorStats, _owner, ref element);
    }

    protected override FacilityCategory GetCategory(FacilityStat stat) {
        return stat.Category;
    }

    protected override FacilityCategory GetCategory(FacilityItem element) {
        return element.category;
    }

    protected override FacilityCategory[] ElementCategories {
        get { return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science }; }
    }

    protected override FacilityCategory[] HQElementCategories {
        get { return new FacilityCategory[] { FacilityCategory.CentralHub }; }
    }

    protected override StarbaseCmdItem MakeCommand(Player owner) {
        LogEvent();
        var countermeasures = _availableCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        StarbaseCmdStat cmdStat = new StarbaseCmdStat(UnitName, 10F, 100, Formation.Circle);

        StarbaseCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseCmdItem>();
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
        _command.HQElement = RandomExtended<AUnitElementItem>.Choice(candidateHQElements) as FacilityItem;
    }

    protected override void __IssueFirstUnitCommand() {
        LogEvent();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

