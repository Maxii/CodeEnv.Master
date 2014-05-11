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
public class SettlementUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, FacilityStats, SettlementCmdModel> {

    private UnitFactory _factory;   // not accesible from AUnitCreator

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    // all starting units are now built and initialized during GameState.PrepareUnitsForOperations

    protected override FacilityStats CreateElementStat(FacilityCategory category, string elementName) {
        FacilityStats stat = new FacilityStats() {
            Category = category,
            Name = elementName,
            Mass = 10000F,
            MaxHitPoints = 50F,
            Strength = new CombatStrength(),
            Weapons = new List<Weapon>() { 
                new Weapon(WeaponCategory.BeamOffense, model: 1) {
                Range = UnityEngine.Random.Range(3F, 5F),
                ReloadPeriod = UnityEngine.Random.Range(0.6F, 0.9F),
                Damage = UnityEngine.Random.Range(3F, 8F) 
                }
            },
        };
        return stat;
    }

    protected override FacilityModel MakeElement(FacilityStats stat, IPlayer owner) {
        return _factory.MakeInstance(stat, owner);
    }

    protected override bool MakeElement(FacilityStats stat, IPlayer owner, ref FacilityModel element) {
        _factory.MakeInstance(stat, owner, ref element);
        return true;    // IMPROVE dummy return for facilities to match signature of abstract MakeElement - facilties currently don't have a HumanView 
    }

    protected override FacilityCategory GetCategory(FacilityStats stat) {
        return stat.Category;
    }

    protected override FacilityCategory[] GetValidElementCategories() {
        return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science };
    }

    protected override FacilityCategory[] GetValidHQElementCategories() {
        return new FacilityCategory[] { FacilityCategory.CentralHub };
    }

    protected override SettlementCmdModel MakeCommand(IPlayer owner) {
        SettlementCmdStats cmdStats = new SettlementCmdStats() {
            Name = UnitName,
            MaxHitPoints = 10F,
            MaxCmdEffectiveness = 100,
            Strength = new CombatStrength(),
            UnitFormation = Formation.Circle,
            Population = 100,
            CapacityUsed = 10,
            ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
            SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F))
        };

        SettlementCmdModel cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<SettlementCmdModel>();
            var existingCmdReference = cmd;
            bool isCmdCompatibleWithOwner = _factory.MakeSettlementCmdInstance(cmdStats, owner, ref cmd);
            if (!isCmdCompatibleWithOwner) {
                Destroy(existingCmdReference.gameObject);
            }
        }
        else {
            cmd = _factory.MakeSettlementCmdInstance(cmdStats, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
    }

    protected override void DeployUnit() {
        var allSystems = SystemCreator.AllSystems; // = __UniverseInitializer.systemModels;
        var availableSystems = allSystems.Where(sys => sys.Data.Owner == TempGameValues.NoPlayer);
        if (availableSystems.IsNullOrEmpty()) {
            //D.Log("Destroying {0} for {1}.", GetType().Name, UnitName);
            Destroy(gameObject);
            return;
        }
        availableSystems.First().AssignSettlement(_command);
    }


    protected override void BeginElementsOperations() {
        _elements.ForAll(e => (e as FacilityModel).CurrentState = FacilityState.Idling);
    }

    protected override void BeginCommandOperations() {
        _command.CurrentState = SettlementState.Idling;
    }

    protected override void AssignHQElement() {
        var candidateHQElements = _command.Elements.Where(e => GetValidHQElementCategories().Contains((e as FacilityModel).Data.Category));
        D.Assert(!candidateHQElements.IsNullOrEmpty()); // bases must have a CentralHub, even if preset
        _command.HQElement = RandomExtended<IElementModel>.Choice(candidateHQElements) as FacilityModel;
    }

    protected override void __InitializeCommandIntel() {
        // For now settlements assume the intel coverage of their system when assigned
    }

    protected override void IssueFirstUnitCommand() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

