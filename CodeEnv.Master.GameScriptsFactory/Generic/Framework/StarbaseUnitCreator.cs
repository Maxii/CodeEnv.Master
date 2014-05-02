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
public class StarbaseUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, FacilityStats, StarbaseCmdModel> {

    private UnitFactory _factory;

    protected override void Awake() {
        base.Awake();
        _factory = UnitFactory.Instance;
    }

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSystems;
    }

    protected override void CreateElementStat(FacilityCategory category, string elementName) {
        FacilityStats stat = new FacilityStats() {
            Category = category,
            Name = elementName,
            Mass = 10000F,
            MaxHitPoints = 50F,
            Strength = new CombatStrength(),
            Weapons = new List<Weapon>() { 
                new Weapon(WeaponCategory.BeamOffense, model: 1) {
                Range = UnityEngine.Random.Range(2F, 4F),
                ReloadPeriod = UnityEngine.Random.Range(1.5F, 2.0F),
                Damage = UnityEngine.Random.Range(1F, 1.5F)
                }
            },
        };
        _elementStats.Add(stat);
    }

    protected override int GetStatsCount(FacilityCategory elementCategory) {
        return _elementStats.Where(s => s.Category == elementCategory).Count();
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

    protected override StarbaseCmdModel MakeCommand(IPlayer owner) {
        StarbaseCmdStats cmdStats = new StarbaseCmdStats() {
            Name = UnitName,
            MaxHitPoints = 10F,
            MaxCmdEffectiveness = 100,
            Strength = new CombatStrength(),
            UnitFormation = Formation.Circle
        };

        StarbaseCmdModel cmd;
        if (isCompositionPreset) {
            cmd = gameObject.GetSafeMonoBehaviourComponentInChildren<StarbaseCmdModel>();
            var existingCmdReference = cmd;
            bool isCmdCompatibleWithOwner = _factory.MakeStarbaseCmdInstance(cmdStats, owner, ref cmd);
            if (!isCmdCompatibleWithOwner) {
                Destroy(existingCmdReference.gameObject);
            }
        }
        else {
            cmd = _factory.MakeStarbaseCmdInstance(cmdStats, owner);
            UnityUtility.AttachChildToParent(cmd.gameObject, gameObject);
        }
        return cmd;
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

