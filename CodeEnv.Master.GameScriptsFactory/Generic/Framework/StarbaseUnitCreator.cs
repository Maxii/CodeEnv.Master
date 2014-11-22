﻿// --------------------------------------------------------------------------------------------------------------------
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
public class StarbaseUnitCreator : AUnitCreator<FacilityItem, FacilityCategory, FacilityData, FacilityStat, StarbaseCommandItem> {

    private static UnitFactory _factory;   // IMPROVE move back to AUnitCreator using References.IUnitFactory?

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
        return _factory.MakeFacilityInstance(stat, Topography.OpenSpace, weaponStats, owner);
    }

    protected override void MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref FacilityItem element) {
        _factory.MakeFacilityInstance(stat, Topography.OpenSpace, weaponStats, owner, ref element);
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

    protected override StarbaseCommandItem MakeCommand(IPlayer owner) {
        LogEvent();
        StarbaseCmdStat cmdStat = new StarbaseCmdStat(UnitName, 10F, 100, Formation.Circle, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F));

        StarbaseCommandItem cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseCommandItem>();
            _factory.MakeInstance(cmdStat, owner, ref cmd);
        }
        else {
            cmd = _factory.MakeInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }


    protected override bool DeployUnit() {
        LogEvent();
        // Starbases don't need to be deployed. They are already on location
        //PathfindingManager.Instance.Graph.UpdateGraph(_command);  // TODO Not yet implemented
        return true;
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

    protected override void EnableOtherWhenRunning() {
        gameObject.GetSafeMonoBehaviourComponentsInChildren<WeaponRangeMonitor>().ForAll(wrt => wrt.enabled = true);
        // CameraLosChangedListener enabled state handled in Item.InitializeViewMembersOnDiscernible
        // Revolvers handle their own enabled state
        // Cmd sprites enabled when shown
        // no orbits present
        // TODO SensorRangeTracker
    }

    protected override void __IssueFirstUnitCommand() {
        LogEvent();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

