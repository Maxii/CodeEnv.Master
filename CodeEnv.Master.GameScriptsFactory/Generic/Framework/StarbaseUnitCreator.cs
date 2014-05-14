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
public class StarbaseUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, FacilityStat, StarbaseCmdModel> {

    private UnitFactory _factory;   // not accesible from AUnitCreator

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStat CreateElementStat(FacilityCategory category, string elementName) {
        return new FacilityStat(elementName, 10000F, 50F, category);
    }

    protected override FacilityModel MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner) {
        return _factory.MakeInstance(stat, weaponStats, owner);
    }

    protected override bool MakeElement(FacilityStat stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref FacilityModel element) {
        _factory.MakeInstance(stat, weaponStats, owner, ref element);
        return true;    // IMPROVE dummy return for facilities to match signature of abstract MakeElement - facilties currently don't have a HumanView 
    }

    protected override FacilityCategory GetCategory(FacilityStat stat) {
        return stat.Category;
    }

    protected override FacilityCategory[] GetValidElementCategories() {
        return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science };
    }

    protected override FacilityCategory[] GetValidHQElementCategories() {
        return new FacilityCategory[] { FacilityCategory.CentralHub };
    }

    protected override StarbaseCmdModel MakeCommand(IPlayer owner) {
        StarbaseCmdStat cmdStat = new StarbaseCmdStat(UnitName, 10F, 100, Formation.Circle, new CombatStrength(0F, 5F, 0F, 5F, 0F, 5F));

        StarbaseCmdModel cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseCmdModel>();
            var existingCmdReference = cmd;
            bool isCmdCompatibleWithOwner = _factory.MakeStarbaseCmdInstance(cmdStat, owner, ref cmd);
            if (!isCmdCompatibleWithOwner) {
                Destroy(existingCmdReference.gameObject);
            }
        }
        else {
            cmd = _factory.MakeStarbaseCmdInstance(cmdStat, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override void DeployUnit() {
        // Starbases don't need to be deployed. They are already on location.
    }

    protected override void BeginElementsOperations() {
        _elements.ForAll(e => (e as FacilityModel).CurrentState = FacilityState.Idling);
    }

    protected override void BeginCommandOperations() {
        _command.CurrentState = StarbaseState.Idling;
    }

    protected override void AssignHQElement() {
        var candidateHQElements = _command.Elements.Where(e => GetValidHQElementCategories().Contains((e as FacilityModel).Data.Category));
        D.Assert(!candidateHQElements.IsNullOrEmpty()); // bases must have a CentralHub, even if preset
        _command.HQElement = RandomExtended<IElementModel>.Choice(candidateHQElements) as FacilityModel;
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    }

    protected override void IssueFirstUnitCommand() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

