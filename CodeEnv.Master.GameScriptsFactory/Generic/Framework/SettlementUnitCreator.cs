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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Initialization class that deploys a Settlement that is available for assignment to a System.
///  When assigned, the Settlement relocates to the orbital slot for Settlements held open by the System.
/// </summary>
public class SettlementUnitCreator : AUnitCreator<FacilityItem, FacilityCategory, FacilityData, FacilityStat, SettlementCommandItem> {

    private static UnitFactory _factory;   // IMPROVE move back to AUnitCreator using References.IUnitFactory?

    public bool orbitMoves;

    protected override void Awake() {
        base.Awake();
        if (_factory == null) {
            _factory = UnitFactory.Instance;
        }
    }

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStat CreateElementStat(FacilityCategory category, string elementName) {
        return new FacilityStat(elementName, 10000F, 50F, category);
    }

    protected override FacilityItem MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner) {
        return _factory.MakeFacilityInstance(stat, SpaceTopography.System, weaponStats, owner);
    }

    protected override void MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref FacilityItem element) {
        _factory.MakeFacilityInstance(stat, SpaceTopography.System, weaponStats, owner, ref element);
    }

    protected override FacilityCategory GetCategory(FacilityStat stat) {
        return stat.Category;
    }

    protected override FacilityCategory GetCategory(FacilityItem element) {
        return element.category;
    }

    protected override FacilityCategory[] GetValidElementCategories() {
        return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science };
    }

    protected override FacilityCategory[] GetValidHQElementCategories() {
        return new FacilityCategory[] { FacilityCategory.CentralHub };
    }

    protected override SettlementCommandItem MakeCommand(IPlayer owner) {
        LogEvent();
        SettlementCmdStat cmdStat = new SettlementCmdStat(UnitName, 10F, 100, Formation.Circle, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F), 100);

        SettlementCommandItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<SettlementCommandItem>();
            _factory.MakeInstance(cmdStat, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        cmd.__OrbiterMoves = orbitMoves;
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

    protected override void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations());
    }

    protected override void BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
    }

    protected override void AssignHQElement() {
        LogEvent();
        var candidateHQElements = _command.Elements.Where(e => GetValidHQElementCategories().Contains((e as FacilityItem).Data.Category));
        D.Assert(!candidateHQElements.IsNullOrEmpty()); // bases must have a CentralHub, even if preset
        _command.HQElement = RandomExtended<AUnitElementItem>.Choice(candidateHQElements) as FacilityItem;
    }

    protected override void __SetIntelCoverage() {
        // Settlements assume the intel coverage of their assigned system
    }

    protected override void EnableOtherWhenRunning() {
        D.Assert(GameStatus.Instance.IsRunning);
        // the entire settlementUnit gameobject has already been detached from this creator at this point
        GameObject settlementUnitGo = _command.transform.parent.gameObject;
        settlementUnitGo.GetSafeMonoBehaviourComponentsInChildren<WeaponRangeMonitor>().ForAll(wrt => wrt.enabled = true);
        // CameraLosChangedListener enabled state handled by Item.InitializeViewMembersOnDiscernible
        // Revolvers handle their own enabled state
        // Cmd sprites enabled when shown
        // settlementUnitGo.GetSafeMonoBehaviourComponentInParents<Orbit>().enabled = true;    // currently keeping Settlements in a fixed location
        // no other orbits present,
        // TODO SensorRangeTracker
    }

    protected override void IssueFirstUnitCommand() {
        LogEvent();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }


}

