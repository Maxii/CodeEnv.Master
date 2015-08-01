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

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
///  Initialization class that deploys a Settlement that is available for assignment to a System.
///  When assigned, the Settlement relocates to the orbital slot for Settlements held open by the System.
/// </summary>
public class SettlementUnitCreator : AUnitCreator<FacilityItem, FacilityCategory, FacilityData, FacilityStat, SettlementCmdItem> {

    public bool orbitMoves;

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStat CreateElementStat(FacilityCategory category, string elementName) {
        float science = category == FacilityCategory.Laboratory ? 10F : Constants.ZeroF;
        float culture = category == FacilityCategory.CentralHub || category == FacilityCategory.Colonizer ? 2.5F : Constants.ZeroF;
        float income = __GetIncome(category);
        float expense = __GetExpense(category);
        return new FacilityStat(elementName, 10000F, 50F, category, science, culture, income, expense);
    }

    protected override FacilityItem MakeElement(FacilityStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
        IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats) {
        return _factory.MakeInstance(stat, Topography.System, wStats, passiveCmStats, activeCmStats, sensorStats, _owner);
    }

    protected override void PopulateElement(FacilityStat stat, IEnumerable<WeaponStat> wStats, IEnumerable<PassiveCountermeasureStat> passiveCmStats,
        IEnumerable<ActiveCountermeasureStat> activeCmStats, IEnumerable<SensorStat> sensorStats, ref FacilityItem element) {
        _factory.PopulateInstance(stat, Topography.System, wStats, passiveCmStats, activeCmStats, sensorStats, _owner, ref element);
    }

    protected override FacilityCategory GetCategory(FacilityStat stat) {
        return stat.Category;
    }

    protected override FacilityCategory GetCategory(FacilityItem element) {
        return element.category;
    }

    protected override FacilityCategory[] ElementCategories {
        get {
            return new FacilityCategory[] { FacilityCategory.Factory, FacilityCategory.Defense, 
            FacilityCategory.Economic, FacilityCategory.Laboratory, FacilityCategory.Barracks, FacilityCategory.Colonizer };
        }
    }

    protected override FacilityCategory[] HQElementCategories {
        get { return new FacilityCategory[] { FacilityCategory.CentralHub }; }
    }

    protected override SettlementCmdItem MakeCommand(Player owner) {
        LogEvent();
        var countermeasures = _availablePassiveCountermeasureStats.Shuffle().Take(countermeasuresPerCmd);
        SettlementCmdStat cmdStat = new SettlementCmdStat(UnitName, 10F, 100, Formation.Circle, 100);

        SettlementCmdItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeFirstMonoBehaviourInChildren<SettlementCmdItem>();
            _factory.PopulateInstance(cmdStat, countermeasures, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, countermeasures, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        cmd.__OrbitSimulatorMoves = orbitMoves;
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
        _command.HQElement = RandomExtended.Choice(candidateHQElements) as FacilityItem;
    }

    protected override void __IssueFirstUnitCommand() {
        LogEvent();
    }

    private float __GetIncome(FacilityCategory category) {
        switch (category) {
            case FacilityCategory.CentralHub:
                return 20F;
            case FacilityCategory.Economic:
                return 100F;
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
                return 5F;
            case FacilityCategory.Defense:
            case FacilityCategory.Factory:
            case FacilityCategory.Laboratory:
                return 10F;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(category));
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

