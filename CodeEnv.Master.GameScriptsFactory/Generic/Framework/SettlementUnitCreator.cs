// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementUnitCreator.cs
//  Initialization class that deploys a Settlement that is available for assignment to a System.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///  Initialization class that deploys a Settlement that is available for assignment to a System.
///  When assigned, the Settlement relocates to the orbital slot for Settlements held open by the System.
/// </summary>
public class SettlementUnitCreator : AUnitCreator<FacilityItem, FacilityCategory, FacilityData, FacilityStat, SettlementCmdItem> {

    public bool orbitMoves;

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStat CreateElementStat(FacilityCategory category, string elementName) {
        return new FacilityStat(elementName, 10000F, 50F, category);
    }

    protected override FacilityItem MakeElement(FacilityStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats) {
        return _factory.MakeInstance(stat, Topography.System, wStats, cmStats, sensorStats, _owner);
    }

    protected override void PopulateElement(FacilityStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<CountermeasureStat> cmStats, IEnumerable<SensorStat> sensorStats, ref FacilityItem element) {
        _factory.PopulateInstance(stat, Topography.System, wStats, cmStats, sensorStats, _owner, ref element);
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

    protected override SettlementCmdItem MakeCommand(Player owner) {
        LogEvent();
        var countermeasures = _availableCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        SettlementCmdStat cmdStat = new SettlementCmdStat(UnitName, 10F, 100, Formation.Circle, 100);

        SettlementCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<SettlementCmdItem>();
            _factory.PopulateInstance(cmdStat, countermeasures, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, countermeasures, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        cmd.__OrbiterMoves = orbitMoves;
        cmd.IsTrackingLabelEnabled = enableTrackingLabel;
        return cmd;
    }

    protected override bool DeployUnit() {
        LogEvent();
        var allSystems = SystemCreator.AllSystems;
        var availableSystems = allSystems.Where(sys => sys.Settlement == null);
        if (availableSystems.Any()) {
            availableSystems.First().Settlement = _command;
            return true;
        }
        D.Warn("No Systems available to deploy {0}.", UnitName);
        return false;
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

